---
id: product-slider-accept-2
story: product-slider
stage: document
kind: acceptance
asked: 2026-09-25T14:27:48Z
digest: 772b074535e4b8669a0d67884688a9057c10331ae6ad3cfe39930c5ebb314969
---

# Accept product-slider?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- shows-discover-products-slider-below-hero: Given the shop has started and seeded its sample catalog; When the shopper opens the homepage; Then a slider headed "Discover products" is shown directly below the hero; And it comes before the section "Why Shop With Us" — `DcaShop.E2eTests.HomeSliderE2eTest#ShowsTheDiscoverProductsSliderDirectlyBelowTheHero`
- slider-holds-eight-different-products: Given the shop has started and seeded its sample catalog; When the shopper opens the homepage; Then the slider holds 8 product cards; And each card shows a different product of the sample catalog — `DcaShop.E2eTests.HomeSliderE2eTest#HoldsEightDifferentProductsOfTheSampleCatalog`
- products-are-drawn-anew-per-request: Given the shopper has opened the homepage and noted the 8 products in the slider; When they reload the homepage 10 times; Then at least one reload shows a different selection of products — `DcaShop.E2eTests.HomeSliderE2eTest#DrawsItsProductsAnewOnEveryRequest`
- product-without-price-is-not-offered: Given a product of the catalogue has no price; When the shopper opens the homepage; Then that product is not among the cards of the slider — `DcaShop.IntegrationTests.ProductSliderTest#AProductWithoutAPriceIsNotAmongTheSlidersCards`
- shows-the-priced-products-there-are: Given exactly 2 products of the catalogue have a price; When the shopper opens the homepage; Then the slider holds 2 product cards, one for each of them — `DcaShop.IntegrationTests.ProductSliderTest#WithTwoPricedProductsTheSliderHoldsACardForEachOfThem`
- card-shows-image-name-and-price: Given the shopper has opened the homepage; When they look at a card of the slider; Then it shows the product's image, its name and the price its product page shows — `DcaShop.E2eTests.HomeSliderE2eTest#ACardShowsTheProductsImageNameAndPrice`
- card-links-to-product-page: Given the shopper has opened the homepage; When they follow the link of a card; Then the product page of that card's product is shown — `DcaShop.E2eTests.HomeSliderE2eTest#ACardLeadsToItsProductPage`
- desktop-shows-four-cards-side-by-side: Given the shop has started and seeded its sample catalog; When the shopper opens the homepage on the desktop; Then the first 4 cards are in view side by side and the other 4 are not; And "Previous" is disabled and "Next" is enabled — `DcaShop.E2eTests.HomeSliderE2eTest#OnTheDesktopTheFirstFourCardsStandSideBySide`
- size-l-shows-four-cards-side-by-side: Given the shop has started and seeded its sample catalog; When the shopper opens the homepage on size l; Then the first 4 cards are in view side by side and the other 4 are not — `DcaShop.E2eTests.HomeSliderTabletE2eTest#OnALargeTabletTheFirstFourCardsStandSideBySide`
- size-m-shows-two-cards-side-by-side: Given the shop has started and seeded its sample catalog; When the shopper opens the homepage on size m; Then the first 2 cards are in view side by side and the other 6 are not — `DcaShop.E2eTests.HomeSliderSmallTabletE2eTest#OnASmallTabletTheFirstTwoCardsStandSideBySide`
- phone-shows-one-card-at-a-time: Given the shop has started and seeded its sample catalog; When the shopper opens the homepage on a phone; Then only the first card is in view; And "Previous" is disabled — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneOnlyTheFirstCardIsInView`
- desktop-next-moves-by-one-card: Given the shopper has opened the homepage on the desktop and the first 4 cards are in view; When they press "Next"; Then the second to the fifth card are in view and the first is not; And "Previous" is enabled — `DcaShop.E2eTests.HomeSliderE2eTest#OnTheDesktopNextMovesTheSliderOnByOneCard`
- desktop-next-is-disabled-at-the-last-card: Given the shopper has opened the homepage on the desktop; When they press "Next" four times; Then the fifth to the eighth card are in view; And "Next" is disabled — `DcaShop.E2eTests.HomeSliderE2eTest#OnTheDesktopTheSliderStopsAtItsLastCard`
- next-brings-the-following-card-into-view: Given the shopper has opened the homepage on a phone and the first card is in view; When they press "Next"; Then the second card is in view instead of the first — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneNextBringsTheSecondCardIntoView`
- previous-brings-the-preceding-card-into-view: Given the shopper on a phone has pressed "Next" once and the second card is in view; When they press "Previous"; Then the first card is in view again — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhonePreviousBringsTheFirstCardBackIntoView`
- next-is-operable-by-keyboard: Given the shopper on a phone has moved the keyboard focus to "Next" with the Tab key; When they press Enter; Then the second card is in view instead of the first — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneNextWorksFromTheKeyboard`
- next-is-disabled-at-the-last-card: Given the shopper has opened the homepage on a phone; When they press "Next" seven times; Then the eighth card is in view; And "Next" is disabled — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneTheSliderStopsAtItsLastCard`
- slider-does-not-move-by-itself: Given the shopper has opened the homepage on a phone and the first card is in view; When they wait 10 seconds without touching the slider; Then the first card is still in view — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#TheSliderDoesNotMoveByItself`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-09-25T14:31:06Z
rationale: 8 products, paged by one card, 4 on xl and l, 2 on m, 1 on s — as corrected.
