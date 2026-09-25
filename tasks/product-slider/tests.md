# Tests — product-slider

<!-- gate:tests -->
| criterion | test |
| --- | --- |
| shows-discover-products-slider-below-hero | DcaShop.E2eTests.HomeSliderE2eTest#ShowsTheDiscoverProductsSliderDirectlyBelowTheHero |
| slider-holds-four-different-products | DcaShop.E2eTests.HomeSliderE2eTest#HoldsFourDifferentProductsOfTheSampleCatalog |
| products-are-drawn-anew-per-request | DcaShop.E2eTests.HomeSliderE2eTest#DrawsItsProductsAnewOnEveryRequest |
| product-without-price-is-not-offered | DcaShop.IntegrationTests.ProductSliderTest#AProductWithoutAPriceIsNotAmongTheSlidersCards |
| shows-the-priced-products-there-are | DcaShop.IntegrationTests.ProductSliderTest#WithTwoPricedProductsTheSliderHoldsACardForEachOfThem |
| card-shows-image-name-and-price | DcaShop.E2eTests.HomeSliderE2eTest#ACardShowsTheProductsImageNameAndPrice |
| card-links-to-product-page | DcaShop.E2eTests.HomeSliderE2eTest#ACardLeadsToItsProductPage |
| desktop-shows-four-cards-side-by-side | DcaShop.E2eTests.HomeSliderE2eTest#OnTheDesktopTheFourCardsStandSideBySide |
| phone-shows-one-card-at-a-time | DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneOnlyTheFirstCardIsInView |
| next-brings-the-following-card-into-view | DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneNextBringsTheSecondCardIntoView |
| previous-brings-the-preceding-card-into-view | DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhonePreviousBringsTheFirstCardBackIntoView |
| next-is-operable-by-keyboard | DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneNextWorksFromTheKeyboard |
| next-is-disabled-at-the-last-card | DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneTheSliderStopsAtItsLastCard |
| slider-does-not-move-by-itself | DcaShop.E2eTests.HomeSliderPhoneE2eTest#TheSliderDoesNotMoveByItself |

## Files
- tests/DcaShop.E2eTests/HomeSliderE2eTest.cs
- tests/DcaShop.E2eTests/HomeSliderPhoneE2eTest.cs
- tests/DcaShop.E2eTests/Pages/HomePage.cs
- tests/DcaShop.E2eTests/Pages/ProductDetailPage.cs
- tests/DcaShop.IntegrationTests/ProductSliderTest.cs
- tests/DcaShop.UnitTests/Product/ProductSelectionTest.cs
- tests/DcaShop.UnitTests/Product/GetProductSelectionUseCaseTest.cs
- tests/DcaShop.UnitTests/Specification/SharedScenariosTest.cs
- src/DcaShop.Product/Domain/Model/ProductSelection.cs
- src/DcaShop.Product/Application/GetProductSelection/GetProductSelectionQuery.cs
- src/DcaShop.Product/Application/GetProductSelection/GetProductSelectionResult.cs
- src/DcaShop.Product/Application/GetProductSelection/IGetProductSelectionInputPort.cs
- src/DcaShop.Product/Application/GetProductSelection/GetProductSelectionUseCase.cs

## Notes
- Decision product-slider-01 answered a (shop-owner, 2026-09-25T12:06:06Z): the readers unescape the display-name
  literal, the scenario title stays as agreed. The dca-factory gate does so since 0.33.4 (already installed, not
  touched here). Changed test of this story, authorised by that answered test-stage decision:
  `tests/DcaShop.UnitTests/Specification/SharedScenariosTest.cs` now turns `\"` into `"` and `\\` into `\` in the
  DisplayName read from source before comparing it with the scenario titles (new `Unescape`); its expectation is
  unchanged otherwise. With `-p:SpecificationPath=…` it passes, so `The homepage shows a "Discover products" slider
  directly below the hero` is bound to `HomeSliderE2eTest#ShowsTheDiscoverProductsSliderDirectlyBelowTheHero`.
- Carrier: in-session, following the `e2e-testing` craft (`carrier.test`) — page objects over `data-test`
  selectors, explicit waits (`WaitForFunctionAsync` on the rendered boxes), one flow per test. Knowledge source
  `dca-knowledge` (profile) was not consulted: no architecture decision was taken in this stage beyond the plan's.
- Browser tests run against the shop at `E2E_BASE_URL` (the suite's existing convention, `E2eFactAttribute`); the
  gate was run with a shop started by `dotnet run --project src/DcaShop.Web` on `http://localhost:5080`.
- Every browser test currently fails on `Assert.True(await home.ShowSliderAsync(), "the homepage shows the product
  slider")`, because the homepage renders no `data-test="product-slider"` section yet (no view component, no
  markup). Behind that assertion each test asserts its scenario's own `Then`/`And` lines.
- `ACardShowsTheProductsImageNameAndPrice` compares the card with the product page through the new
  `data-test="product-detail-title"` / `"product-detail-price"` (plan: Detail.cshtml change, build stage) and the
  product page's image inside `data-test="product-detail"`.
- `TheSliderDoesNotMoveByItself` waits a real 10 s (`Page.WaitForTimeoutAsync`): the wait is the observation, as
  the plan says, and a fake clock (`Page.Clock`) would not catch a CSS animation or native smooth scrolling that
  moves the track on its own.
- In-view is judged from the rendered boxes: a card is in view when its whole box lies within the slider's box and
  the window's width (`HomePage.CardInView`); the slider is scrolled into the window first, so the desktop checks do
  not depend on the window's height.
- ProductSliderTest#AProductWithoutAPriceIsNotAmongTheSlidersCards and #WithTwoPricedProductsTheSliderHoldsACardForEachOfThem
  replace `IPricingDataPort` with a test answer that prices only chosen seeded products (the state of a product
  nobody has priced) and read the cards from `GET /`; they fail on `Assert.Equal(4|2, cards.Count)` (actual 0),
  because the homepage has no slider yet.
- guard (no criterion key, story Assumptions line 2): ProductSliderTest#WithoutAnyPricedProductTheHomepageShowsNoSlider
  passes today — the story says this "holds already and is not a criterion; the plan guards it". It is a
  regression guard for the build stage, not a red proof.
- unit tests: ProductSelectionTest#DrawsFourDifferentProductsFromTheCandidates (≤ 4, distinct, only candidates),
  #DrawsEveryCandidateWhenThereAreFewerThanFour, #DrawsNothingWithoutCandidates,
  #DifferentRandomSourcesDrawDifferentSelections (drawn anew), #TheSameRandomSourceDrawsTheSameSelection (the draw
  depends only on the random source) for invariant "a product selection holds up to four different priced
  products"; GetProductSelectionUseCaseTest#OffersOnlyProductsThatHaveAPrice, #OffersAtMostFourPricedProducts,
  #OffersAPricedProductThatIsOutOfStock (story Assumptions line 3), #KeepsTheOrderTheProductsWereDrawnIn,
  #OffersNothingWhenNoProductHasAPrice. All fail on `NotImplementedException` from the stubs.
- `#KeepsTheOrderTheProductsWereDrawnIn` assumes the use case hands `ProductSelection.Draw` the priced candidates in
  catalog order (`IProductRepository.FindAllAsync`, name order) — the plan's "candidates = ids present in the
  answer" over `FindAllAsync`.
- stubs (compile only, each throws `NotImplementedException`): `src/DcaShop.Product/Domain/Model/ProductSelection.cs`,
  `src/DcaShop.Product/Application/GetProductSelection/{GetProductSelectionQuery,GetProductSelectionResult,IGetProductSelectionInputPort,GetProductSelectionUseCase}.cs`.
  Not registered in DI, no view component, no markup — the build stage's.

