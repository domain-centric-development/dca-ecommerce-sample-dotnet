# Build — product-slider

Round: acceptance correction product-slider-accept-1 (8 cards; 4 in view on xl/l, 2 on m, 1 on s; paging by one
card). Carrier: `dca-modelling` (`carrier.build`) — the change is one constant of an existing value object, no new
tactical type. Guard: `dca-discipline` (`carrier.guard`) — the domain stays framework-free; nothing crosses a
context. Knowledge source `dca-knowledge` (profile) was not consulted: the build took no architecture decision
beyond the plan's, which rests on the nodes the plan names.

## Changed
| File | Why |
| --- | --- |
| src/DcaShop.Product/Domain/Model/ProductSelection.cs | `MaxSize` 4 → 8: the selection holds up to eight products |
| src/DcaShop.Web/wwwroot/css/main.css | `.product-slider__card` two per view at size m (`@media (max-width: 768px)`, before the existing 480 px one-per-view rule); four per view on l and xl stays the default |
| src/DcaShop.Product/Application/GetProductSelection/GetProductSelectionUseCase.cs | first delivery (50730b9): the read use case drawing priced products; unchanged this round, takes the size from the draw |
| src/DcaShop.Product/Adapter/Incoming/Web/ProductSliderViewComponent.cs | first delivery (50730b9): the fragment adapter; unchanged this round |
| src/DcaShop.Product/Adapter/Incoming/Web/ProductSliderViewModel.cs | first delivery (50730b9): the fragment's view model; unchanged this round |
| src/DcaShop.Product/Infrastructure/ProductContextRegistration.cs | first delivery (50730b9): registers the use case; unchanged this round |
| src/DcaShop.Web/Views/Home/Index.cshtml | first delivery (50730b9): composes the slider below the hero; unchanged this round |
| src/DcaShop.Web/Views/Product/Detail.cshtml | first delivery (50730b9): `data-test` selectors on title and price for the card-to-page comparison; unchanged this round |
| src/DcaShop.Web/Views/Shared/Components/ProductSlider/Default.cshtml | first delivery (50730b9): the fragment and its paging script; unchanged this round, its one-card step and end detection already hold for 8 cards |

## Criteria
- shows-discover-products-slider-below-hero: met by the unchanged fragment below the hero.
- slider-holds-eight-different-products: met by `ProductSelection.Draw` drawing up to 8 distinct candidates (8 of the 21 seeded).
- products-are-drawn-anew-per-request: met by the per-request draw, now of 8 of 21.
- product-without-price-is-not-offered: met by the use case offering only priced candidates; 10 priced → 8 cards.
- shows-the-priced-products-there-are: met by the draw taking every candidate when there are fewer than 8.
- card-shows-image-name-and-price: met by the unchanged card markup.
- card-links-to-product-page: met by the unchanged card link.
- desktop-shows-four-cards-side-by-side: met by the default four-per-view width; with 8 cards the track overflows, so the script enables "Next".
- size-l-shows-four-cards-side-by-side: met by the default four-per-view width (1024 px is above the 768 px rule).
- size-m-shows-two-cards-side-by-side: met by the new 768 px rule, two cards per view.
- phone-shows-one-card-at-a-time: met by the existing 480 px rule, one card per view.
- desktop-next-moves-by-one-card: met by the unchanged script scrolling one card step.
- desktop-next-is-disabled-at-the-last-card: met by the script's end detection after four steps (8 − 4).
- next-brings-the-following-card-into-view: met by the unchanged script.
- previous-brings-the-preceding-card-into-view: met by the unchanged script.
- next-is-operable-by-keyboard: met by the unchanged button order (controls before the track).
- next-is-disabled-at-the-last-card: met by the script's end detection after seven steps on a phone.
- slider-does-not-move-by-itself: met by the script having no timer.

## Deviations from the plan
- None. The paging script needed no sub-pixel fix at any of the four sizes.
- Note: `main.css` is a copy of the Java sample's stylesheet (AGENTS.md); the new size-m rule makes it diverge until
  the Java twin follows with its own story (story `## Out of scope`, the specification's exception). The document
  stage should record this in the sync notes; `src/DcaShop.Product/Domain/glossary.md` ("Up to four") is its item too.

## Checks
- dotnet build: 0 errors
- dotnet test tests/DcaShop.UnitTests: 176 passed, 4 skipped (specification switch off)
- dotnet test tests/DcaShop.IntegrationTests: 58 passed, 1 skipped
- E2E_BASE_URL=http://localhost:5080 dotnet test tests/DcaShop.E2eTests --filter HomeSlider: 16 passed
- E2E_BASE_URL=http://localhost:5080 dotnet test tests/DcaShop.E2eTests: 37 passed, 1 skipped (a first full run had
  "Logout redirects to login page and shows logout confirmation" fail once; it passed alone and in the full rerun —
  unrelated to the slider, flaky)
- dotnet test tests/DcaShop.ArchitectureTests: 129 passed
- dotnet format (formatFix) then dotnet format --verify-no-changes: clean, no file reformatted
