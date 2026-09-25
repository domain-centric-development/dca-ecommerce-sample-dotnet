# Plan — product-slider: Product slider on the homepage (correction product-slider-accept-1)

Carrier: in-session; the profile names no `carrier.plan`. Knowledge source: `dca-knowledge` (named in
`.agents/factory/factory.profile.yaml`), bundle `../dca-knowledge-catalog/bundle/`. This run takes no new
architecture decision: the correction changes a domain constant, the stylesheet, the fragment's layout and the
tests; the shape of the delivered slider (read use case, fragment adapter, value object) stays. The nodes the
first delivery rested on still hold for it and are not re-decided here: `/decision/read-model-vs-domain-query.md`,
`/recipe/add-a-read-model.md`, `/rule/hexagonal/dca-hex-011.md`, `/rule/hexagonal/dca-hex-007.md`.

This plan plans the **change from the existing implementation** (delivered in commit `50730b9`), not the slider
from scratch. Decision product-slider-accept-1 answered a correction: "8 random products with a price (was 4),
paged by Previous/Next one card at a time; visible at once 4 on `xl` and `l`, 2 on `m`, 1 on `s`; Previous is
disabled at the start, Next at the end; no auto-play; everything else as the story says" — it lands in the
constant, the stylesheet and the criteria below. Decision product-slider-01 (answer a, applied) still holds: the
readers unescape the DisplayName literal, so `shows-discover-products-slider-below-hero` keeps its quoted title.

## Context

`Product` (Product Catalog, Core — `project/domain.md:21`). Story `context: Product`; story Assumptions line 1:
the slider lives in `Product`, composed into the homepage by name like the mini basket. Designed map
(`project/domain.md:55-70`) and generated map (`docs/architecture/context-map.md:27,40-59`) agree on everything
this story touches; the correction adds no context and no relationship. The actor (shopper) reaches the surface
already: the homepage `GET /` with the delivered fragment (`src/DcaShop.Web/Views/Home/Index.cshtml`,
`Component.InvokeAsync("ProductSlider")`). No new page, endpoint, REST resource or MCP tool.

Project description: `project/product.md:37-44` names the four sizes — `s` up to 480 px, `m` up to 768 px, `l` up to
1180 px, `xl` above. The criteria name sizes only; the size boundaries become the stylesheet's breakpoints.
`project/tech.md` `## Frontend approach`: server-rendered, one stylesheet, no client framework — the delivered
inline paging script stays plain script.

## What already holds, and what changes

The delivered paging script (`src/DcaShop.Web/Views/Shared/Components/ProductSlider/Default.cshtml:29-53`) is
already size-independent: `step()` is the distance between two cards, `move(±1)` scrolls by one step, `update()`
disables Previous at `scrollLeft <= 1` and Next when the track's end is reached. With 8 cards and 4 in view
the track overflows, so Next becomes enabled and four presses reach the end (max scroll = 4 steps). What makes
the desktop show "all cards, both buttons disabled" today is only that there were 4 cards for 4 slots
(build.md, criterion desktop-shows-four-cards-side-by-side). So the correction is:

1. `ProductSelection.MaxSize` 4 → 8 (`src/DcaShop.Product/Domain/Model/ProductSelection.cs:10`). The use case
   (`GetProductSelectionUseCase.cs:31`) and the view component take the size from the draw; nothing else counts.
   The seeded catalogue has 21 products, all priced at creation (`SampleDataSeeder.cs:22-66`, `CreateProductCommand`
   with a price), so 8 of 21 are drawn and a reload still differs.
2. Stylesheet: cards per view by size. Today `.product-slider__card { flex: 0 0 calc((100% - 3 * gap) / 4) }`
   (`main.css:2849-2850`) and one card at `max-width: 480px` (`main.css:2915-2919`). Add a rule for size `m`
   — `@media (max-width: 768px)`: two cards per view, `calc((100% - var(--product-slider-gap)) / 2)` — placed
   before the 480 px rule so `s` still wins. `l` keeps the default four; the file's existing size breakpoints
   are the same (`main.css:3514`, `4263`, `4291` use 768 / 769–1180).
3. Nothing in the script, the view component, the view model, the use case's flow or the homepage view changes.
   Should the build stage find the script's end detection off by a sub-pixel at a size (rounding of the
   `calc` widths), the fix belongs in `update()` of the same view, not in a new mechanism.

## Changes

| Element | Kind | Location | New or changed |
| --- | --- | --- | --- |
| `ProductSelection` | Value object — `MaxSize` 4 → 8; XML doc unchanged in shape ("Up to `MaxSize` …") | `src/DcaShop.Product/Domain/Model/ProductSelection.cs` | changed |
| Slider styles | `.product-slider__card` two per view at size `m` (`@media (max-width: 768px)`), one at `s` (existing 480 px rule, kept after it), four at `l` and `xl` (default) | `src/DcaShop.Web/wwwroot/css/main.css` (section 28, `.product-slider__*`) | changed |
| `GetProductSelectionUseCase` | unchanged — draws `ProductSelection.MaxSize` | `src/DcaShop.Product/Application/GetProductSelection/` | unchanged |
| Slider fragment view + paging script | unchanged — already one card per step and disabled at both ends | `src/DcaShop.Web/Views/Shared/Components/ProductSlider/Default.cshtml` | unchanged (see point 3 above) |

Selectors stay as delivered: `product-slider`, `product-slider-title`, `product-slider-card`,
`product-slider-card-image`, `-name`, `-price`, `-link`, `product-slider-previous`, `product-slider-next`.

## Acceptance criteria

Browser tests run in `tests/DcaShop.E2eTests` (Playwright, `browser: playwright`) against `E2E_BASE_URL` and carry the
scenario title of `../dca-sample-specification/scenarios.md` (`scenario.home.slider-*`, lines 186-303, sixteen
scenarios) **verbatim** as `DisplayName` — `SharedScenariosTest` fails on a title without a test or a test without a
title. "On the desktop" = size xl = the suite's default browser window (`BaseE2eTest.cs:55`, `ContextOptions => null`,
Playwright's default 1280 px). Phone = size s = 393 × 852 (`HomeSliderPhoneE2eTest.cs:17`). Size l and size m get a
test class each with a viewport inside the size: l = 1024 × 768 (spec: 769 to 1180 px); m = 768 × 1024, the upper
edge of m, which is the width the human's correction names ("from about 760px"). The two criteria that need a
catalogue other than the seeded one have no shared scenario and stay HTTP-level tests in
`tests/DcaShop.IntegrationTests` (story Assumptions, "Does the browser test need …").

### Rule: Directly below the hero the homepage shows a slider headed "Discover products"

- shows-discover-products-slider-below-hero: With the seeded catalogue, the homepage shows a slider headed exactly
  "Discover products" directly below the hero, before "Why Shop With Us" (`data-test="features"`).
  →  browser test, Playwright, default window; DisplayName `The homepage shows a "Discover products" slider directly below the hero` — unchanged test

### Rule: The slider holds up to eight random products that have a price, drawn anew on every request

- slider-holds-eight-different-products: With the seeded catalogue the slider holds 8 product cards, each a
  different product of the sample catalogue.
  →  browser test, Playwright, default window; DisplayName `The homepage slider holds eight different products of the sample catalog`
  (`scenario.home.slider-eight-different-products`); invariant unit-tested on `ProductSelection` (≤ 8, distinct, only candidates)
- products-are-drawn-anew-per-request: After noting the 8 products, 10 reloads show a different selection at least once.
  →  browser test, Playwright; DisplayName `The homepage slider draws its products anew on every request`; unit test with different seeds
- product-without-price-is-not-offered: A product without a price is not among the cards.
  →  HTTP-level test in `tests/DcaShop.IntegrationTests` (`ProductSliderTest`, stubbed `IPricingDataPort`) — with
  more than 8 priced products so the draw is a strict choice, asserting 8 cards, all priced
- shows-the-priced-products-there-are: With exactly 2 priced products the slider holds 2 cards, one for each.
  →  HTTP-level test, `ProductSliderTest` — unchanged test

### Rule: A card shows the product's image, name and price and leads to its product page

- card-shows-image-name-and-price: A card shows the product's image, name and the price its product page shows.
  →  browser test, Playwright; DisplayName `A slider card shows the product's image, name and price` — unchanged test
- card-links-to-product-page: Following a card's link shows that product's page.
  →  browser test, Playwright; DisplayName `A slider card leads to its product page` — unchanged test

### Rule: Four cards are in view side by side on sizes xl and l, two on size m, one on size s

- desktop-shows-four-cards-side-by-side: On the desktop (xl) the first 4 cards are in view side by side (same top,
  increasing left) and the other 4 are not; "Previous" is disabled and "Next" is enabled.
  →  browser test, Playwright, default window; DisplayName `On the desktop the slider shows four of its cards side by side` (`scenario.home.slider-desktop`)
- size-l-shows-four-cards-side-by-side: On size l the first 4 cards are in view side by side and the other 4 are not.
  →  browser test, Playwright, 1024 × 768; DisplayName `On a large tablet the slider shows four of its cards side by side` (`scenario.home.slider-tablet-four-cards`)
- size-m-shows-two-cards-side-by-side: On size m the first 2 cards are in view side by side and the other 6 are not.
  →  browser test, Playwright, 768 × 1024; DisplayName `On a small tablet the slider shows two of its cards side by side` (`scenario.home.slider-small-tablet-two-cards`)
- phone-shows-one-card-at-a-time: On a phone (s) only the first card is in view and "Previous" is disabled.
  →  browser test, Playwright, 393 px; DisplayName `On a phone the slider shows one card at a time` — unchanged test

### Rule: Previous and Next move by one card, by mouse or keyboard, and are disabled at the start and at the end

- desktop-next-moves-by-one-card: On the desktop with the first 4 cards in view, pressing "Next" brings the second
  to the fifth card into view and the first out of it; "Previous" is enabled.
  →  browser test, Playwright, default window; DisplayName `On the desktop Next moves the slider on by one card` (`scenario.home.slider-desktop-next`)
- desktop-next-is-disabled-at-the-last-card: On the desktop, after "Next" four times, the fifth to the eighth card
  are in view and "Next" is disabled.
  →  browser test, Playwright, default window; DisplayName `On the desktop the slider stops at its last card` (`scenario.home.slider-desktop-stops-at-the-end`)
- next-brings-the-following-card-into-view: On a phone, "Next" brings the second card into view instead of the first.
  →  browser test, 393 px; DisplayName `On a phone Next brings the following slider card into view` — unchanged test
- previous-brings-the-preceding-card-into-view: On a phone, after "Next" once, "Previous" brings the first card back.
  →  browser test, 393 px; DisplayName `On a phone Previous brings the preceding slider card back into view` — unchanged test
- next-is-operable-by-keyboard: On a phone, focus moved to "Next" with Tab, Enter brings the second card into view.
  →  browser test, 393 px; DisplayName `On a phone the slider's Next button works from the keyboard` — unchanged test
- next-is-disabled-at-the-last-card: On a phone, after "Next" seven times, the eighth card is in view and "Next" is disabled.
  →  browser test, 393 px; DisplayName `On a phone the slider stops at its last card` (`scenario.home.slider-stops-at-the-end`)
- slider-does-not-move-by-itself: On a phone, after 10 s untouched, the first card is still in view.
  →  browser test, 393 px; DisplayName `The homepage slider does not move by itself` — unchanged test

Details the story specifies, held by the criteria: 8 cards; 4 / 4 / 2 / 1 in view on xl / l / m / s; a step of exactly
one card; Previous disabled at the first card, Next at the last, both enabled in between (desktop-next: "Previous"
enabled); no auto-play. Guard without a key (story Assumptions line 2): no priced product → no slider section —
`ProductSliderTest#WithoutAnyPricedProductTheHomepageShowsNoSlider`, unchanged.

## Changed tests

| Test file | Backed by |
| --- | --- |
| `tests/DcaShop.E2eTests/HomeSliderE2eTest.cs` — `HoldsFourDifferentProductsOfTheSampleCatalog` (expects 4 cards, DisplayName "…four different products…") becomes the eight-card test with the new title; `DrawsItsProductsAnewOnEveryRequest` (`Assert.Equal(4, noted.Count)`) expects 8; `OnTheDesktopTheFourCardsStandSideBySide` (all 4 in view, both buttons disabled) expects cards 0–3 in view and 4–7 not, Previous disabled, Next enabled, new title; the two new desktop tests are added to this class | "The slider holds up to 8 products; before, it held up to 4." and "On the desktop the slider shows 4 of its 8 cards, "Next" is enabled and moves by one card; before, all its cards were in view side by side and "Previous" and "Next" were both disabled." |
| `tests/DcaShop.E2eTests/HomeSliderPhoneE2eTest.cs` — `OnAPhoneTheSliderStopsAtItsLastCard` (three presses, fourth card, Next disabled) expects seven presses, the eighth card | "On a phone "Next" is disabled after seven presses, at the eighth card; before, after three, at the fourth." |
| `tests/DcaShop.E2eTests/Pages/HomePage.cs` — the page object gains a settled "enabled" check for a button (today `SettlesDisabledAsync` only waits for `disabled`; asserting "Next is enabled" through it would wait out its 5 s timeout) and, if the tests want it, a helper for "cards i..j in view" | "On the desktop the slider shows 4 of its 8 cards, "Next" is enabled and moves by one card; …" |
| `tests/DcaShop.UnitTests/Product/ProductSelectionTest.cs` — `DrawsFourDifferentProductsFromTheCandidates` (4 of 21) expects 8 of 21; `DrawsEveryCandidateWhenThereAreFewerThanFour` renamed to fewer than eight (expectation with 2 candidates unchanged); the "same four products" message of `DifferentRandomSourcesDrawDifferentSelections` | "The slider holds up to 8 products; before, it held up to 4." |
| `tests/DcaShop.UnitTests/Product/GetProductSelectionUseCaseTest.cs` — `OffersAtMostFourPricedProducts` (10 priced → 4) expects 8, renamed | "The slider holds up to 8 products; before, it held up to 4." |
| `tests/DcaShop.IntegrationTests/ProductSliderTest.cs` — `AProductWithoutAPriceIsNotAmongTheSlidersCards` prices 5 products and expects 4 cards (`:35,42`); with up to 8 it would get 5 — prices more than 8 (e.g. 10 of the 21) and expects 8 cards, all priced | "The slider holds up to 8 products; before, it held up to 4." |

Not contradicted and not listed: `GetProductSelectionUseCaseTest#KeepsTheOrderTheProductsWereDrawnIn` (8 products →
all drawn, still in draw order; holds), `#OffersOnlyProductsThatHaveAPrice`, `#OffersAPricedProductThatIsOutOfStock`,
`#OffersNothingWhenNoProductHasAPrice`, `ProductSliderTest#WithTwoPricedProducts…`, `#WithoutAnyPricedProduct…`,
`tests/DcaShop.UnitTests/Specification/SharedScenariosTest.cs` (reads titles from the specification; no change).

## Files

- `src/DcaShop.Product/Domain/Model/ProductSelection.cs` — changes: `MaxSize = 8`.
- `src/DcaShop.Web/wwwroot/css/main.css` — changes: `.product-slider__card` two per view at `max-width: 768px`, before the existing 480 px rule.
- `tests/DcaShop.E2eTests/HomeSliderE2eTest.cs` — changes: eight-card and desktop expectations and titles; two new tests (desktop-next, desktop-stops-at-the-end).
- `tests/DcaShop.E2eTests/HomeSliderPhoneE2eTest.cs` — changes: stops-at-the-end with seven presses / eighth card.
- `tests/DcaShop.E2eTests/HomeSliderTabletE2eTest.cs` — changes: new, size l (1024 × 768), `size-l-shows-four-cards-side-by-side`.
- `tests/DcaShop.E2eTests/HomeSliderSmallTabletE2eTest.cs` — changes: new, size m (768 × 1024), `size-m-shows-two-cards-side-by-side`.
- `tests/DcaShop.E2eTests/Pages/HomePage.cs` — changes: settled "enabled" check (and optional range helper).
- `tests/DcaShop.UnitTests/Product/ProductSelectionTest.cs` — changes: 8 instead of 4.
- `tests/DcaShop.UnitTests/Product/GetProductSelectionUseCaseTest.cs` — changes: at most 8.
- `tests/DcaShop.IntegrationTests/ProductSliderTest.cs` — changes: more than 8 priced, 8 cards expected.
- `src/DcaShop.Web/Views/Shared/Components/ProductSlider/Default.cshtml` — read: the paging script that already moves one card and disables at both ends (change only if the end detection proves off at a size).
- `src/DcaShop.Product/Application/GetProductSelection/GetProductSelectionUseCase.cs`, `src/DcaShop.Product/Adapter/Incoming/Web/ProductSliderViewComponent.cs` — read: take the size from the draw; no change.
- `src/DcaShop.Product/Adapter/Incoming/Bootstrap/SampleDataSeeder.cs:22-66` — read: 21 seeded, priced products (8 drawn of 21).
- `tests/DcaShop.E2eTests/BaseE2eTest.cs:55`, `HomeSliderPhoneE2eTest.cs:17`, `MobileLayoutE2eTest.cs:18` — read: default window and the viewport override to mirror for l and m.
- `project/product.md:37-44` — read: the size table (the breakpoints).
- `../dca-sample-specification/scenarios.md:186-303`, `../dca-sample-specification/exceptions.md` — read: the sixteen titles, the .NET-first exception.
- `src/DcaShop.Product/Domain/glossary.md:108-112` — document stage: "Up to four different products" → eight.

## Glossary proposals

None new. The existing `ProductSelection` entry (`src/DcaShop.Product/Domain/glossary.md:108-112`) says "Up to four
different products"; the document stage changes it to eight. The Portal entry "Product slider" names no count.

## Open assumptions

- Default Playwright window (1280 px, no override in `BaseE2eTest`) is size xl, as the story's size note says.
- Viewports for l (1024) and m (768, the upper edge of m) are the test stage's choice inside the specification's
  ranges; the criteria name sizes only. A test at the lower edges (769, 481) is not asked for.
- "In view" stays as the page object judges it today: whole card inside the slider's box and the window's width
  (`HomePage.cs:26-37`); a card scrolled out of the track lies outside the section's box, so "the other 4 are not
  in view" is observable with the existing check.
- The keyboard path on a phone is unchanged: the controls stand before the track (build.md "Deviations"), so Tab
  reaches "Next" without passing card links.
- `README.md:156-158` and `planning/porting-status.md` name the slider without a count; the Java twin follows with its
  own story (story `## Out of scope`, `exceptions.md`). Nothing to plan there.
- Harness answer (AGENTS.md principle 1): catalog node — none new (the UI-composition gap was already recorded by the
  first delivery); rule — none (responsive card counts are a presentation property, not a dependency rule); marker —
  none.
