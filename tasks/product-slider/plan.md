# Plan — product-slider: Product slider on the homepage

Carrier: in-session; the profile names no `carrier.plan`. Knowledge source: `dca-knowledge` (named in
`.agents/factory/factory.profile.yaml`), bundle `dca-knowledge-catalog/bundle/`, `bundle_sha256`
`851654624570dd8d75bb00cb604b6a560698d6a54ae53e4edce39be956cc956c`.

## Context

`Product` (Product Catalog, Core subdomain — `project/domain.md`, "Bounded Contexts"). The story's
`context: Product`, and the domain contact's answer (story `## Assumptions`, first line; brief "Answer 1"):
the slider lives in `Product`; the homepage shows it the way it shows the mini basket, and Portal references
no other context.

- On the designed map (`project/domain.md`) and on the generated one (`docs/architecture/context-map.md`)
  `Product` exists with the same relationships: it consumes `Pricing` and `Inventory` through their Api (ACL)
  and is in a Partnership with both. The two maps agree on everything this story touches. The only
  difference is the one `project/domain.md` states itself (the Account → Product identity dependency runs
  through the shared kernel and is invisible to the generated map); it is not touched here.
- No new context and no new relationship. `Portal` stays Separate Ways: it composes in the UI
  (`project/domain.md`, "Notable design choices" 5), and `src/DcaShop.Portal/DcaShop.Portal.csproj`
  references only `DcaShop.SharedKernel`. The homepage view invokes the slider fragment by name, exactly as
  `_Layout.cshtml:35` invokes `Component.InvokeAsync("MiniBasket")` — no C# reference from Portal to Product.
- The actor (shopper) already reaches the surface: the homepage `GET /`
  (`src/DcaShop.Portal/Adapter/Incoming/Web/HomePageController.cs:8-9`, view
  `src/DcaShop.Web/Views/Home/Index.cshtml`). The criteria name that surface themselves ("the shopper opens
  the homepage"), and the fragment is specified by the domain contact (brief "Answer 2": "Composed like the
  mini basket (a view component the page invokes)"). No new page, endpoint, REST resource or MCP tool —
  the REST API and the MCP server are out of scope.
- Project description: `project/product.md` `## Surfaces` lists the home page on desktop and phone;
  `## Look and feel` asks for one shared stylesheet, keyboard use and `data-test` on every element a test
  addresses. `project/tech.md` `## Frontend approach`: server-rendered Razor, no client framework — so the
  paging is a few lines of plain inline script, as the layout's theme switch already is
  (`src/DcaShop.Web/Views/Shared/_Layout.cshtml:81-98`). `## Persistence`: in memory, nothing new is stored.

### Where each decision comes from

- Where the slider lives, how it is composed — story Assumptions line 1; brief Answers 1 and 2.
- What "has a price" means — Pricing's answer through the catalog's own `IPricingDataPort`
  (`src/DcaShop.Product/Application/Shared/IPricingDataPort.cs:8-13`); `PricingDataAdapter` states "A product
  without a price record is simply absent from the answer" (`Adapter/Outgoing/Pricing/PricingDataAdapter.cs:9`).
  Today `ProductArticleAssembler.ArticleOf` turns an absent price into `Money.Zero`
  (`Application/Shared/ProductArticleAssembler.cs:35`), so an `EnrichedProduct` can no longer tell "no price"
  apart; the new use case therefore selects on the pricing answer *before* it enriches (see Changes).
- Out-of-stock products are offered as long as they have a price — story Assumptions line 3.
- Not shown at all when no product has a price — story Assumptions line 2 (guarded, not a criterion).
- Random draw, anew per request, no popularity, no personalisation — story Rule 2 and `## Out of scope`.
- Knowledge: a plain query use case over the repository, not a dedicated read model —
  `/decision/read-model-vs-domain-query.md` (review: reviewed; "the plain query use case is the default").
  Read shape: `/recipe/add-a-read-model.md` (reviewed) — Query/Result in the application package, the
  enriched model as the cross-context value, a primitives-only ViewModel in `adapter/incoming/web`.
  Incoming adapter depends on the input-port interface, not the use case — `/rule/hexagonal/dca-hex-011.md`;
  incoming adapter depends on no other module — `/rule/hexagonal/dca-hex-007.md`; ViewModel in the web
  adapter package — `/rule/naming/dca-nam-011.md`; Result carries values only — `DCA-USE-015` (AGENTS.md).
  The catalog has **no** node on UI composition / view components across contexts (searched for
  "UI composition", "view component", "fragment", "mini basket": no hit); the placement below follows the
  project's own precedent (`MiniBasketViewComponent`) and the domain contact's answer, not a catalog node.

## Changes

| Element | Kind | Location | New or changed |
| --- | --- | --- | --- |
| `ProductSelection` | Value object (`sealed record … : IValue`) — up to 4 distinct product ids drawn at random from the priced candidates; `Draw(IReadOnlyCollection<ProductId> pricedProducts, Random random)`; `MaxSize = 4`; fewer candidates → all of them; no candidates → empty | `src/DcaShop.Product/Domain/Model/ProductSelection.cs` (namespace `DcaShop.Product.Domain.Model`) | new |
| `GetProductSelectionQuery` | Query (empty record) | `src/DcaShop.Product/Application/GetProductSelection/` | new |
| `GetProductSelectionResult` | Result — `IReadOnlyList<EnrichedProduct> Products`, in draw order (values only, like `GetAllProductsResult`) | same folder | new |
| `IGetProductSelectionInputPort` | Input port `: IUseCase<GetProductSelectionQuery, GetProductSelectionResult>` | same folder | new |
| `GetProductSelectionUseCase` | Read use case, no transaction: `IProductRepository.FindAllAsync` → `IPricingDataPort.GetPricesAsync(all ids)` → candidates = ids present in the answer → `ProductSelection.Draw` → `ProductArticleAssembler.EnrichAsync(drawn products)` → result in draw order. `Random` is a constructor dependency so unit tests can seed it | same folder | new |
| `ProductSliderViewComponent` | Incoming web adapter (ASP.NET Core `ViewComponent`, invoked by name `"ProductSlider"`) — depends only on `IGetProductSelectionInputPort`; maps to the view model; renders nothing when the selection is empty | `src/DcaShop.Product/Adapter/Incoming/Web/ProductSliderViewComponent.cs` | new |
| `ProductSliderViewModel` | ViewModel, primitives only: list of `Card(Guid ProductId, string Name, string ImageUrl, string Price)`; `Price` formatted exactly as `ProductPageController` formats the detail price (`p.CurrentPrice.ToString()`, `ProductPageController.cs:44`) | `src/DcaShop.Product/Adapter/Incoming/Web/ProductSliderViewModel.cs` | new |
| Product context registration | register `IGetProductSelectionInputPort` → `GetProductSelectionUseCase` (scoped) with `Random.Shared` | `src/DcaShop.Product/Infrastructure/ProductContextRegistration.cs` | changed |
| Slider fragment view | Razor view of the component: `<section data-test="product-slider">`, heading "Discover products", a track of cards (image — or the catalog's letter placeholder when `ImageUrl` is empty, as `Catalog.cshtml:22` does —, name, price, link to `/products/{id}`), `<button>` "Previous" and "Next" (native buttons: focusable, Enter/Space activate them, `disabled` attribute at the ends), inline plain script that moves by one card and sets `disabled` from the scroll position; no timer, no auto-play | `src/DcaShop.Web/Views/Shared/Components/ProductSlider/Default.cshtml` (views live in the host, as `Views/Shared/Components/MiniBasket/Default.cshtml`) | new |
| Homepage view | `@await Component.InvokeAsync("ProductSlider")` directly after the hero section (`Index.cshtml:10`) and before `data-test="features"` ("Why Shop With Us", `Index.cshtml:11-12`) | `src/DcaShop.Web/Views/Home/Index.cshtml` | changed |
| Product detail view | add `data-test="product-detail-title"` to the `<h1>` (`Detail.cshtml:12`) and `data-test="product-detail-price"` to the price value span (`Detail.cshtml:34`) so a test can compare the card with the product page; markup otherwise unchanged | `src/DcaShop.Web/Views/Product/Detail.cshtml` | changed |
| Slider styles | `.product-slider` block: 4 cards side by side at the default desktop width, one card per view at ≤ 480 px wide (393 px phone), horizontal track with overflow hidden / scroll-snap, button states; no animation that moves by itself | `src/DcaShop.Web/wwwroot/css/main.css` (the one shared stylesheet, `product.md` `## Look and feel`) | changed |

Selectors (`data-test`), for the page object and the markup alike: `product-slider`, `product-slider-title`,
`product-slider-card`, `product-slider-card-image`, `product-slider-card-name`, `product-slider-card-price`,
`product-slider-card-link`, `product-slider-previous`, `product-slider-next`.

Not changed: `EnrichedProduct`, `ProductArticle`, `ProductArticleAssembler`, `ProductCatalogService` (Api),
`HomePageController`, Portal's project references, any REST resource or MCP tool.

## Acceptance criteria

Browser tests run in `tests/DcaShop.E2eTests` (Playwright, `browser: playwright` in the profile) and carry
the **scenario title** of `../dca-sample-specification/scenarios.md` as `DisplayName` — `SharedScenariosTest`
(`tests/DcaShop.UnitTests/Specification/SharedScenariosTest.cs:17-36`) fails on a browser test without a
scenario. The two criteria that need a catalogue other than the seeded one have no shared scenario (story
Assumptions, last line) and therefore are **not** browser tests: they run at HTTP level against the wired
application (`WebApplicationFactory<Program>` with `ConfigureTestServices`, as
`tests/DcaShop.IntegrationTests/IntegrationEventDeliveryTest.cs:21-27` does), in `tests/DcaShop.IntegrationTests`.
Phone = 393 × 852 viewport, as `tests/DcaShop.E2eTests/MobileLayoutE2eTest.cs:18`.

### Rule: Directly below the hero the homepage shows a slider headed "Discover products"

- shows-discover-products-slider-below-hero: With the seeded sample catalog, opening the homepage shows a
  slider headed exactly "Discover products" directly below the hero (the next section after
  `data-test="hero"`), and it comes before the section "Why Shop With Us" (`data-test="features"`).
  →  test shape: browser test, Playwright in `tests/DcaShop.E2eTests`, DisplayName
  "The homepage shows a "Discover products" slider directly below the hero" (`scenario.home.slider-below-hero`)

### Rule: The slider holds up to four random products that have a price, drawn anew on every request

- slider-holds-four-different-products: With the seeded catalog, the slider holds 4 product cards, and each
  card shows a different product of the sample catalog.
  →  test shape: browser test, Playwright, DisplayName
  "The homepage slider holds four different products of the sample catalog" (`scenario.home.slider-four-different-products`);
  invariant also unit-tested on `ProductSelection` (≤ 4, distinct, only candidates) in `tests/DcaShop.UnitTests`
- products-are-drawn-anew-per-request: After noting the 4 products, reloading the homepage 10 times shows a
  different selection at least once.
  →  test shape: browser test, Playwright, DisplayName
  "The homepage slider draws its products anew on every request" (`scenario.home.slider-drawn-anew`);
  unit test on `ProductSelection` with two different seeds
- product-without-price-is-not-offered: A product of the catalogue that has no price is not among the
  slider's cards.
  →  test shape: HTTP-level test in `tests/DcaShop.IntegrationTests` — `GET /` against the application with a
  test `IPricingDataPort` that leaves chosen seeded products unpriced, repeated over several requests,
  asserting none of their ids appears in a `product-slider-card-link`; plus a `GetProductSelectionUseCase` unit
  test with stub ports. What it cannot show: the rendering in a browser (not needed — the rule is about which
  cards are in the HTML)
- shows-the-priced-products-there-are: With exactly 2 products priced, the slider holds 2 product cards, one
  for each of them.
  →  test shape: HTTP-level test in `tests/DcaShop.IntegrationTests` (same stubbed pricing port, pricing 2
  products), asserting exactly 2 cards with exactly those 2 product ids

### Rule: A card shows the product's image, name and price and leads to its product page

- card-shows-image-name-and-price: A card shows the product's image, its name and the price the product
  page shows for that product (same string, as the detail page renders it).
  →  test shape: browser test, Playwright, DisplayName
  "A slider card shows the product's image, name and price" (`scenario.home.slider-card-content`) — reads a
  card's image, name and price, opens its product page and compares with `product-detail-title` /
  `product-detail-price`
- card-links-to-product-page: Following a card's link shows the product page of that card's product.
  →  test shape: browser test, Playwright, DisplayName
  "A slider card leads to its product page" (`scenario.home.slider-card-link`) — URL `/products/{id}` of the
  card, `product-detail-title` equals the card name

### Rule: On the desktop the four cards stand side by side

- desktop-shows-four-cards-side-by-side: In the suite's default browser window all 4 cards are in view,
  side by side (same top, increasing left), and "Previous" and "Next" are both disabled.
  →  test shape: browser test, Playwright (default context, no viewport override), DisplayName
  "On the desktop the slider shows its four cards side by side" (`scenario.home.slider-desktop`)

### Rule: On a phone one card is in view; Previous and Next move by one card, by mouse or keyboard, and are disabled at the start and at the end

- phone-shows-one-card-at-a-time: On a phone only the first card is in view and "Previous" is disabled.
  →  test shape: browser test, Playwright, 393 px viewport, DisplayName
  "On a phone the slider shows one card at a time" (`scenario.home.slider-phone-one-card`)
- next-brings-the-following-card-into-view: On a phone, with the first card in view, pressing "Next" brings
  the second card into view instead of the first.
  →  test shape: browser test, Playwright, 393 px, DisplayName
  "On a phone Next brings the following slider card into view" (`scenario.home.slider-next`)
- previous-brings-the-preceding-card-into-view: On a phone, after "Next" once, pressing "Previous" brings
  the first card into view again.
  →  test shape: browser test, Playwright, 393 px, DisplayName
  "On a phone Previous brings the preceding slider card back into view" (`scenario.home.slider-previous`)
- next-is-operable-by-keyboard: On a phone, with the keyboard focus moved to "Next" with the Tab key,
  pressing Enter brings the second card into view instead of the first.
  →  test shape: browser test, Playwright, 393 px, keyboard only (`Tab` until `product-slider-next` is
  focused, then `Enter`), DisplayName
  "On a phone the slider's Next button works from the keyboard" (`scenario.home.slider-keyboard`)
- next-is-disabled-at-the-last-card: On a phone, pressing "Next" three times brings the fourth card into view
  and "Next" is disabled.
  →  test shape: browser test, Playwright, 393 px, DisplayName
  "On a phone the slider stops at its last card" (`scenario.home.slider-stops-at-the-end`)
- slider-does-not-move-by-itself: On a phone, with the first card in view, after 10 seconds without touching
  the slider the first card is still in view.
  →  test shape: browser test, Playwright, 393 px, DisplayName
  "The homepage slider does not move by itself" (`scenario.home.slider-no-auto-play`) — the one place a
  timed wait is the observation itself, not a synchronisation

Details the story specifies, held by the criteria above: the heading text "Discover products"; placement
directly below the hero and before "Why Shop With Us"; 4 cards; the button labels "Previous" and "Next";
"disabled" as the state at the ends (native `disabled` attribute); move by exactly one card; no auto-play;
no add-to-cart button on the card (card leads to the product page only).

Guard without a criterion key (story Assumptions line 2 — "the plan guards it"): when no product has a price,
the homepage renders no `product-slider` section at all. Test shape: HTTP-level test in
`tests/DcaShop.IntegrationTests` with the stubbed pricing port answering nothing; plus the use-case unit test
for the empty selection.

## Files

- `src/DcaShop.Product/Domain/Model/ProductSelection.cs` — changes: new value object, the random draw.
- `src/DcaShop.Product/Application/GetProductSelection/GetProductSelectionQuery.cs` — changes: new.
- `src/DcaShop.Product/Application/GetProductSelection/GetProductSelectionResult.cs` — changes: new.
- `src/DcaShop.Product/Application/GetProductSelection/IGetProductSelectionInputPort.cs` — changes: new.
- `src/DcaShop.Product/Application/GetProductSelection/GetProductSelectionUseCase.cs` — changes: new.
- `src/DcaShop.Product/Adapter/Incoming/Web/ProductSliderViewComponent.cs` — changes: new fragment adapter.
- `src/DcaShop.Product/Adapter/Incoming/Web/ProductSliderViewModel.cs` — changes: new.
- `src/DcaShop.Product/Infrastructure/ProductContextRegistration.cs` — changes: register the use case.
- `src/DcaShop.Web/Views/Shared/Components/ProductSlider/Default.cshtml` — changes: new slider markup and inline paging script.
- `src/DcaShop.Web/Views/Home/Index.cshtml` — changes: invoke the component between hero and features.
- `src/DcaShop.Web/Views/Product/Detail.cshtml` — changes: `data-test` on title and price.
- `src/DcaShop.Web/wwwroot/css/main.css` — changes: `.product-slider` block, desktop and ≤ 480 px rules.
- `tests/DcaShop.E2eTests/Pages/HomePage.cs` — changes: new page object (slider cards, in-view checks, Previous/Next, keyboard).
- `tests/DcaShop.E2eTests/Pages/ProductDetailPage.cs` — changes: read title and price (new members; existing members unchanged).
- `tests/DcaShop.E2eTests/HomeSliderE2eTest.cs` — changes: new, the desktop scenarios (below-hero, four-different, drawn-anew, card-content, card-link, desktop).
- `tests/DcaShop.E2eTests/HomeSliderPhoneE2eTest.cs` — changes: new, the 393 px scenarios (phone-one-card, next, previous, keyboard, stops-at-the-end, no-auto-play).
- `tests/DcaShop.IntegrationTests/ProductSliderTest.cs` — changes: new, the two non-shared criteria and the no-price guard.
- `tests/DcaShop.UnitTests/Product/ProductSelectionTest.cs` — changes: new, draw invariants.
- `tests/DcaShop.UnitTests/Product/GetProductSelectionUseCaseTest.cs` — changes: new, priced-only selection, draw order kept, empty when nothing is priced.
- `src/DcaShop.Web/ViewComponents/MiniBasketViewComponent.cs`, `src/DcaShop.Web/Views/Shared/Components/MiniBasket/Default.cshtml`, `src/DcaShop.Web/Views/Shared/_Layout.cshtml:35` — read: the composition pattern to mirror (invoked by name, view in the host).
- `src/DcaShop.Web/Program.cs:16-21` — read: `DcaShop.Product` is an MVC application part, so a view component there is discovered without further wiring.
- `src/DcaShop.Product/Application/GetAllProducts/*`, `Application/Shared/ProductArticleAssembler.cs`, `Application/Shared/IPricingDataPort.cs` — read: the read use-case shape and the enrichment to reuse.
- `src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs`, `src/DcaShop.Web/Views/Product/Catalog.cshtml` — read: price formatting and the card markup / image placeholder to mirror.
- `tests/DcaShop.E2eTests/MobileLayoutE2eTest.cs`, `BaseE2eTest.cs`, `Pages/BasePage.cs`, `SeededCatalogE2eTest.cs` — read: viewport override, page-object base, `data-test` helpers.
- `tests/DcaShop.IntegrationTests/IntegrationEventDeliveryTest.cs:21-27`, `ShopFlowTest.cs` — read: `ConfigureTestServices` override and HTML assertions over `GET /`.
- `tests/DcaShop.UnitTests/Specification/SharedScenariosTest.cs` — read: why the browser tests must carry the scenario titles verbatim and why the two non-shared criteria are not browser tests.
- `../dca-sample-specification/scenarios.md` (`scenario.home.slider-*`, lines 186-273), `../dca-sample-specification/exceptions.md` — read: the shared scenario titles and the approved Java exception.

## Glossary proposals

- Product selection (`ProductSelection`, Product context): up to four different products of the catalogue
  that have a price, drawn at random anew on every request; shown on the homepage as the "Discover products"
  slider. Not a popularity ranking and not personalised.
- Product slider (Portal glossary, referenced term owned by `Product`): the homepage section "Discover
  products" that presents the product selection as cards, paged with Previous and Next.

## Open assumptions

- "Has a price" = Pricing's answer contains the product (`IPricingDataPort`), not "price ≠ 0". A priced
  product always has a positive price (Product glossary, "Shared contract revision": Price wraps strictly
  positive Money), so the two agree today; the plan uses the port answer because it is what Pricing states.
- The use case asks the pricing port once for all products (to select) and the assembler asks again for the
  ≤ 4 drawn ones (to enrich). Two in-process calls; accepted rather than changing `ProductArticle`'s shape,
  which would also change `tests/DcaShop.UnitTests/Product/ProductTest.cs:48-49`.
- The random source is `System.Random` (BCL, no framework type), injected into the use case and passed to
  the domain's `ProductSelection.Draw`; production uses `Random.Shared`.
- "In view" is judged against the slider's own visible track and the browser viewport (Playwright's
  in-viewport check plus the track's bounding box), not by CSS class names.
- The paging script is plain inline JavaScript inside the fragment view (no client framework,
  `project/tech.md`); without script the desktop view is unaffected (all four cards are already in view).
- Views and `main.css` are otherwise one-to-one copies of the Java sample's (AGENTS.md "Views mirror the Java
  sample's Pug templates one to one"). This story adds .NET-first markup and CSS under the approved exception
  `scenario.home.slider-*` in `../dca-sample-specification/exceptions.md` (review 2026-10-31); the two new
  `data-test` attributes on the product page and the `.product-slider` CSS are to be carried over by the Java
  twin's story. `planning/porting-status.md` and the Portal/Product glossaries are the document stage's.
- The architecture suite (`tests/DcaShop.ArchitectureTests`) does not scan `DcaShop.Web`, but does scan
  `DcaShop.Product`; the new view component sits in `Adapter/Incoming/Web` and depends only on the input
  port, which `DCA-HEX-011` and `DCA-HEX-007` require. No rule selects view components by name (checked
  `DCA-NAM-005`/`-006`, which select controller stereotypes); the architecture run confirms it.
- Knowledge-catalog gap for the harness (AGENTS.md principle 1): the catalog has no node on UI composition
  of one context's fragment into another context's page. Candidates: a recipe "compose a fragment into
  another context's page", and possibly a rule that a fragment adapter (view component) in
  `adapter/incoming/web` depends only on its own context's input ports (today covered generically by
  `DCA-HEX-007`/`-011`). Marker: none proposed. To be recorded by the document stage, not built here.
