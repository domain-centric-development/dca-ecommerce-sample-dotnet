# Build — product-slider

Carrier: `dca-modelling` (`carrier.build`), applied in-session for `ProductSelection`; guard `dca-discipline`
(`carrier.guard`) kept as described in the stage (framework-free domain, port-only dependency of the incoming
adapter, no cross-context reference from Portal). Knowledge source `dca-knowledge` (profile) was not consulted anew:
no decision beyond the plan's was taken; the plan's nodes (`/decision/read-model-vs-domain-query.md`,
`/recipe/add-a-read-model.md`, `/rule/hexagonal/dca-hex-011.md`, `/rule/hexagonal/dca-hex-007.md`) hold for what was built.

## Changed
| File | Why |
| --- | --- |
| src/DcaShop.Product/Domain/Model/ProductSelection.cs | the random draw: up to four distinct candidates by a partial Fisher–Yates over the given random source, value equality over the drawn ids |
| src/DcaShop.Product/Application/GetProductSelection/GetProductSelectionUseCase.cs | read use case: catalog → Pricing's answer → priced ids in catalog order → draw → enrich in draw order |
| src/DcaShop.Product/Adapter/Incoming/Web/ProductSliderViewComponent.cs | new fragment adapter "ProductSlider", depends only on `IGetProductSelectionInputPort`; renders nothing for an empty selection |
| src/DcaShop.Product/Adapter/Incoming/Web/ProductSliderViewModel.cs | primitives-only view model (id, name, image, price as the product page formats it) |
| src/DcaShop.Product/Infrastructure/ProductContextRegistration.cs | registers the use case (scoped) with `Random.Shared` |
| src/DcaShop.Web/Views/Shared/Components/ProductSlider/Default.cshtml | slider markup with the plan's `data-test` selectors, native Previous/Next buttons, inline paging script (one card per step, disabled at the ends, no timer) |
| src/DcaShop.Web/Views/Home/Index.cshtml | invokes the component between the hero and "Why Shop With Us" |
| src/DcaShop.Web/Views/Product/Detail.cshtml | `data-test="product-detail-title"` and `data-test="product-detail-price"` |
| src/DcaShop.Web/wwwroot/css/main.css | `.product-slider__*` block: four cards side by side, one card at ≤ 480 px, scroll-snap track, no animation |
| tasks/product-slider/build.md | this file |

## Criteria
- shows-discover-products-slider-below-hero: met by the component invoked right after the hero section, heading "Discover products", before `data-test="features"`
- slider-holds-four-different-products: met by `ProductSelection.Draw` (at most four, distinct) over the priced seeded catalog
- products-are-drawn-anew-per-request: met by a scoped use case drawing with `Random.Shared` on every request
- product-without-price-is-not-offered: met by the use case taking only ids present in Pricing's answer as candidates
- shows-the-priced-products-there-are: met by the draw answering every candidate when there are fewer than four
- card-shows-image-name-and-price: met by the card rendering the product's `ImageUrl`, name and `CurrentPrice.ToString()` — the detail page's string
- card-links-to-product-page: met by the card link `/products/{id}`
- desktop-shows-four-cards-side-by-side: met by the flex track with four cards per view; both buttons disabled because the track does not overflow
- phone-shows-one-card-at-a-time: met by the ≤ 480 px rule (one card per view) and Previous disabled at scroll position 0
- next-brings-the-following-card-into-view: met by Next scrolling the track by one card step
- previous-brings-the-preceding-card-into-view: met by Previous scrolling back by one card step
- next-is-operable-by-keyboard: met by native `<button>`s placed before the track in the tab order
- next-is-disabled-at-the-last-card: met by the scroll handler disabling Next when the track's end is reached
- slider-does-not-move-by-itself: met by the script having no timer and the CSS no animation on the slider

## Deviations from the plan
- Slider fragment view: the Previous/Next controls stand between the heading and the track, not after it. With the
  controls after the track, tabbing to "Next" passes the four card links first, and the browser scrolls a focused
  card into view — the track stood at the last card before Enter was pressed (keyboard test red). Controls first keeps
  the keyboard path to "Next" free of the cards.
- Slider styles: own `.product-slider__*` card classes instead of reusing `.product-card`, because `.product-card`
  carries the `fadeInUp` entry animation and a hover lift — motion the story rules out and which moves card tops
  during the desktop check.
- `ProductSelection` overrides `Equals`/`GetHashCode` so the value compares by its ids (a record over a list would
  compare by reference). Not named in the plan; part of the value-object shape.

## Checks
- dotnet build: 0 errors
- dotnet test tests/DcaShop.UnitTests --logger trx: 176 passed, 4 skipped (specification tests without `SpecificationPath`)
- dotnet test tests/DcaShop.IntegrationTests --logger trx: 58 passed, 1 skipped (pre-existing)
- E2E_BASE_URL=http://localhost:5080 dotnet test tests/DcaShop.E2eTests --logger trx: 33 passed, 1 skipped (pre-existing framing test); all 12 slider tests green
- dotnet test tests/DcaShop.ArchitectureTests: 129 passed; `docs/architecture/context-map.md` unchanged
- dotnet format (formatFix), then dotnet format --verify-no-changes: clean
