---
id: product-slider-accept-1
story: product-slider
stage: document
kind: acceptance
asked: 2026-09-25T13:59:58Z
digest: 6d0df5ce705bf4ba1af3788a99ae68e1f4b4cd3a25354efdf102fcfc44245a83
---

# Accept product-slider?

## Question
The story was delivered before acceptance existed; the human looked at the delivered slider afterwards. This record
is written for that look (by the decisions skill, for a story already delivered), with the story's digest as it was
delivered:

- shows-discover-products-slider-below-hero — `DcaShop.E2eTests.HomeSliderE2eTest#ShowsTheDiscoverProductsSliderDirectlyBelowTheHero`
- slider-holds-four-different-products — `DcaShop.E2eTests.HomeSliderE2eTest#HoldsFourDifferentProductsOfTheSampleCatalog`
- products-are-drawn-anew-per-request — `DcaShop.E2eTests.HomeSliderE2eTest#DrawsItsProductsAnewOnEveryRequest`
- product-without-price-is-not-offered — `DcaShop.IntegrationTests.ProductSliderTest#AProductWithoutAPriceIsNotAmongTheSlidersCards`
- shows-the-priced-products-there-are — `DcaShop.IntegrationTests.ProductSliderTest#WithTwoPricedProductsTheSliderHoldsACardForEachOfThem`
- card-shows-image-name-and-price — `DcaShop.E2eTests.HomeSliderE2eTest#ACardShowsTheProductsImageNameAndPrice`
- card-links-to-product-page — `DcaShop.E2eTests.HomeSliderE2eTest#ACardLeadsToItsProductPage`
- desktop-shows-four-cards-side-by-side — `DcaShop.E2eTests.HomeSliderE2eTest#OnTheDesktopTheFourCardsStandSideBySide`
- phone-shows-one-card-at-a-time — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneOnlyTheFirstCardIsInView`
- next-brings-the-following-card-into-view — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneNextBringsTheSecondCardIntoView`
- previous-brings-the-preceding-card-into-view — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhonePreviousBringsTheFirstCardBackIntoView`
- next-is-operable-by-keyboard — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneNextWorksFromTheKeyboard`
- next-is-disabled-at-the-last-card — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneTheSliderStopsAtItsLastCard`
- slider-does-not-move-by-itself — `DcaShop.E2eTests.HomeSliderPhoneE2eTest#TheSliderDoesNotMoveByItself`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this
  record); the story runs again from plan.

## Answer
answer: correction: Previous and Next make no sense when four are shown: make it eight and pageable. From about 760px four tiles do not look good any more — rather two.
by: the human
at: 2026-09-25T13:59:58Z
rationale: confirmed as a correction of product-slider — 8 random products with a price (was 4), paged by Previous/Next one card at a time; visible at once: 4 on `xl` and `l`, 2 on `m`, 1 on `s` (the sizes in `project/product.md`); Previous is disabled at the start, Next at the end; no auto-play; everything else as the story says.
