# Tests — product-slider

<!-- gate:tests -->
| criterion | test |
| --- | --- |
| shows-discover-products-slider-below-hero | DcaShop.E2eTests.HomeSliderE2eTest#ShowsTheDiscoverProductsSliderDirectlyBelowTheHero |
| slider-holds-eight-different-products | DcaShop.E2eTests.HomeSliderE2eTest#HoldsEightDifferentProductsOfTheSampleCatalog |
| products-are-drawn-anew-per-request | DcaShop.E2eTests.HomeSliderE2eTest#DrawsItsProductsAnewOnEveryRequest |
| product-without-price-is-not-offered | DcaShop.IntegrationTests.ProductSliderTest#AProductWithoutAPriceIsNotAmongTheSlidersCards |
| shows-the-priced-products-there-are | DcaShop.IntegrationTests.ProductSliderTest#WithTwoPricedProductsTheSliderHoldsACardForEachOfThem |
| card-shows-image-name-and-price | DcaShop.E2eTests.HomeSliderE2eTest#ACardShowsTheProductsImageNameAndPrice |
| card-links-to-product-page | DcaShop.E2eTests.HomeSliderE2eTest#ACardLeadsToItsProductPage |
| desktop-shows-four-cards-side-by-side | DcaShop.E2eTests.HomeSliderE2eTest#OnTheDesktopTheFirstFourCardsStandSideBySide |
| size-l-shows-four-cards-side-by-side | DcaShop.E2eTests.HomeSliderTabletE2eTest#OnALargeTabletTheFirstFourCardsStandSideBySide |
| size-m-shows-two-cards-side-by-side | DcaShop.E2eTests.HomeSliderSmallTabletE2eTest#OnASmallTabletTheFirstTwoCardsStandSideBySide |
| phone-shows-one-card-at-a-time | DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneOnlyTheFirstCardIsInView |
| desktop-next-moves-by-one-card | DcaShop.E2eTests.HomeSliderE2eTest#OnTheDesktopNextMovesTheSliderOnByOneCard |
| desktop-next-is-disabled-at-the-last-card | DcaShop.E2eTests.HomeSliderE2eTest#OnTheDesktopTheSliderStopsAtItsLastCard |
| next-brings-the-following-card-into-view | DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneNextBringsTheSecondCardIntoView |
| previous-brings-the-preceding-card-into-view | DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhonePreviousBringsTheFirstCardBackIntoView |
| next-is-operable-by-keyboard | DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneNextWorksFromTheKeyboard |
| next-is-disabled-at-the-last-card | DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneTheSliderStopsAtItsLastCard |
| slider-does-not-move-by-itself | DcaShop.E2eTests.HomeSliderPhoneE2eTest#TheSliderDoesNotMoveByItself |

## Files
- tests/DcaShop.E2eTests/HomeSliderE2eTest.cs
- tests/DcaShop.E2eTests/HomeSliderPhoneE2eTest.cs
- tests/DcaShop.E2eTests/HomeSliderTabletE2eTest.cs
- tests/DcaShop.E2eTests/HomeSliderSmallTabletE2eTest.cs
- tests/DcaShop.E2eTests/Pages/HomePage.cs
- tests/DcaShop.IntegrationTests/ProductSliderTest.cs
- tests/DcaShop.UnitTests/Product/ProductSelectionTest.cs
- tests/DcaShop.UnitTests/Product/GetProductSelectionUseCaseTest.cs
- tests/DcaShop.UnitTests/Specification/SharedScenariosTest.cs

## Notes
- `SharedScenariosTest.cs` is listed because the story changed it in its first run (decision product-slider-01: the
  DisplayName read from source is unescaped before comparison); this run did not touch it.
- Correction product-slider-accept-1: this run changes the delivered tests to the story's new expectations only where
  the plan's `## Changed tests` lists them; no production code was touched, so no stub was needed (the slider exists,
  `ProductSelection.MaxSize` is still 4 and the stylesheet has no size-m rule).
- Carrier: in-session, following the `e2e-testing` craft (`carrier.test`) — page objects over `data-test` selectors,
  settled waits on the rendered boxes and the button state, one flow per test. Knowledge source `dca-knowledge`
  (profile) was not consulted: this stage took no architecture decision beyond the plan's.
- Browser tests ran against a shop started with `dotnet run --project src/DcaShop.Web` on `http://localhost:5080`
  (`E2E_BASE_URL`), stopped afterwards. DisplayNames are the titles of `../dca-sample-specification/scenarios.md`
  (`scenario.home.slider-*`) verbatim. Viewports: xl = default window (1280 px), l = 1024 × 768, m = 768 × 1024,
  s = 393 × 852.
- HomeSliderE2eTest#HoldsEightDifferentProductsOfTheSampleCatalog, #DrawsItsProductsAnewOnEveryRequest,
  #OnTheDesktopTheFirstFourCardsStandSideBySide, HomeSliderTabletE2eTest#OnALargeTabletTheFirstFourCardsStandSideBySide,
  HomeSliderSmallTabletE2eTest#OnASmallTabletTheFirstTwoCardsStandSideBySide: currently fail on
  `Assert.Equal(8, …)` (actual 4), because `ProductSelection.MaxSize` is 4. Behind it the small-tablet test asserts
  cards 0–1 in view, which also needs the stylesheet's two-per-view rule at size m (today four per view at 768 px).
- HomeSliderE2eTest#OnTheDesktopNextMovesTheSliderOnByOneCard and #OnTheDesktopTheSliderStopsAtItsLastCard: currently
  fail on `"Next" can be pressed` (first press), because with 4 cards for 4 slots the track does not overflow and
  "Next" stays disabled. HomeSliderPhoneE2eTest#OnAPhoneTheSliderStopsAtItsLastCard: fails on `"Next" can be pressed
  a 4. time`, because the fourth card is the last today. The "can be pressed" check before each press (new
  `HomePage.IsNextEnabledAsync`) keeps a disabled button from failing as a 30 s click timeout instead of an assertion.
- `HomePage` gained `IsNextEnabledAsync` / `IsPreviousEnabledAsync` (settled wait for `disabled === false`); the
  existing `SettlesDisabledAsync` would wait out its 5 s timeout when asked about an enabled button.
- Unchanged and green today (regression guards): below-hero, card content, card link, phone one card, phone next,
  phone previous, keyboard, no auto-play; ProductSliderTest#WithTwoPricedProducts…, #WithoutAnyPricedProduct….
- ProductSliderTest#AProductWithoutAPriceIsNotAmongTheSlidersCards: now prices 10 of the 21 seeded products; fails on
  `Assert.Equal(8, cards.Count)` (actual 4), because `ProductSelection.MaxSize` is 4.
- unit tests: ProductSelectionTest#DrawsEightDifferentProductsFromTheCandidates (fails, expected 8, actual 4),
  #DrawsEveryCandidateWhenThereAreFewerThanEight, #DrawsNothingWithoutCandidates,
  #DifferentRandomSourcesDrawDifferentSelections, #TheSameRandomSourceDrawsTheSameSelection for invariant "a product
  selection holds up to eight different candidate products"; GetProductSelectionUseCaseTest#OffersAtMostEightPricedProducts
  (fails, expected 8, actual 4) for "at most eight priced products are offered".
