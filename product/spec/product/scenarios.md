# product

### Rule: Directly below the hero the homepage shows a slider headed "Discover products"

#### shows-discover-products-slider-below-hero
Title: The homepage shows a "Discover products" slider directly below the hero
- Given the shop has started and seeded its sample catalog
- When the shopper opens the homepage
- Then a slider headed "Discover products" is shown directly below the hero
- And it comes before the section "Why Shop With Us"
Test: DcaShop.E2eTests.HomeSliderE2eTest#ShowsTheDiscoverProductsSliderDirectlyBelowTheHero

### Rule: The slider holds up to eight random products that have a price, drawn anew on every request

#### slider-holds-eight-different-products
Title: The homepage slider holds eight different products of the sample catalog
- Given the shop has started and seeded its sample catalog
- When the shopper opens the homepage
- Then the slider holds 8 product cards
- And each card shows a different product of the sample catalog
Test: DcaShop.E2eTests.HomeSliderE2eTest#HoldsEightDifferentProductsOfTheSampleCatalog

#### products-are-drawn-anew-per-request
Title: The homepage slider draws its products anew on every request
- Given the shopper has opened the homepage and noted the 8 products in the slider
- When they reload the homepage 10 times
- Then at least one reload shows a different selection of products
Test: DcaShop.E2eTests.HomeSliderE2eTest#DrawsItsProductsAnewOnEveryRequest

#### product-without-price-is-not-offered
- Given a product of the catalogue has no price
- When the shopper opens the homepage
- Then that product is not among the cards of the slider
Test: DcaShop.IntegrationTests.ProductSliderTest#AProductWithoutAPriceIsNotAmongTheSlidersCards

#### shows-the-priced-products-there-are
- Given exactly 2 products of the catalogue have a price
- When the shopper opens the homepage
- Then the slider holds 2 product cards, one for each of them
Test: DcaShop.IntegrationTests.ProductSliderTest#WithTwoPricedProductsTheSliderHoldsACardForEachOfThem

### Rule: A card shows the product's image, name and price and leads to its product page

#### card-shows-image-name-and-price
Title: A slider card shows the product's image, name and price
- Given the shopper has opened the homepage
- When they look at a card of the slider
- Then it shows the product's image, its name and the price its product page shows
Test: DcaShop.E2eTests.HomeSliderE2eTest#ACardShowsTheProductsImageNameAndPrice

#### card-links-to-product-page
Title: A slider card leads to its product page
- Given the shopper has opened the homepage
- When they follow the link of a card
- Then the product page of that card's product is shown
Test: DcaShop.E2eTests.HomeSliderE2eTest#ACardLeadsToItsProductPage

### Rule: Four cards are in view side by side on sizes xl and l, two on size m, one on size s

#### desktop-shows-four-cards-side-by-side
Title: On the desktop the slider shows four of its cards side by side
- Given the shop has started and seeded its sample catalog
- When the shopper opens the homepage on the desktop
- Then the first 4 cards are in view side by side and the other 4 are not
- And "Previous" is disabled and "Next" is enabled
Test: DcaShop.E2eTests.HomeSliderE2eTest#OnTheDesktopTheFirstFourCardsStandSideBySide

#### size-l-shows-four-cards-side-by-side
Title: On a large tablet the slider shows four of its cards side by side
- Given the shop has started and seeded its sample catalog
- When the shopper opens the homepage on size l
- Then the first 4 cards are in view side by side and the other 4 are not
Test: DcaShop.E2eTests.HomeSliderTabletE2eTest#OnALargeTabletTheFirstFourCardsStandSideBySide

#### size-m-shows-two-cards-side-by-side
Title: On a small tablet the slider shows two of its cards side by side
- Given the shop has started and seeded its sample catalog
- When the shopper opens the homepage on size m
- Then the first 2 cards are in view side by side and the other 6 are not
Test: DcaShop.E2eTests.HomeSliderSmallTabletE2eTest#OnASmallTabletTheFirstTwoCardsStandSideBySide

#### phone-shows-one-card-at-a-time
Title: On a phone the slider shows one card at a time
- Given the shop has started and seeded its sample catalog
- When the shopper opens the homepage on a phone
- Then only the first card is in view
- And "Previous" is disabled
Test: DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneOnlyTheFirstCardIsInView

### Rule: Previous and Next move by one card, by mouse or keyboard, and are disabled at the start and at the end

#### desktop-next-moves-by-one-card
Title: On the desktop Next moves the slider on by one card
- Given the shopper has opened the homepage on the desktop and the first 4 cards are in view
- When they press "Next"
- Then the second to the fifth card are in view and the first is not
- And "Previous" is enabled
Test: DcaShop.E2eTests.HomeSliderE2eTest#OnTheDesktopNextMovesTheSliderOnByOneCard

#### desktop-next-is-disabled-at-the-last-card
Title: On the desktop the slider stops at its last card
- Given the shopper has opened the homepage on the desktop
- When they press "Next" four times
- Then the fifth to the eighth card are in view
- And "Next" is disabled
Test: DcaShop.E2eTests.HomeSliderE2eTest#OnTheDesktopTheSliderStopsAtItsLastCard

#### next-brings-the-following-card-into-view
Title: On a phone Next brings the following slider card into view
- Given the shopper has opened the homepage on a phone and the first card is in view
- When they press "Next"
- Then the second card is in view instead of the first
Test: DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneNextBringsTheSecondCardIntoView

#### previous-brings-the-preceding-card-into-view
Title: On a phone Previous brings the preceding slider card back into view
- Given the shopper on a phone has pressed "Next" once and the second card is in view
- When they press "Previous"
- Then the first card is in view again
Test: DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhonePreviousBringsTheFirstCardBackIntoView

#### next-is-operable-by-keyboard
Title: On a phone the slider's Next button works from the keyboard
- Given the shopper on a phone has moved the keyboard focus to "Next" with the Tab key
- When they press Enter
- Then the second card is in view instead of the first
Test: DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneNextWorksFromTheKeyboard

#### next-is-disabled-at-the-last-card
Title: On a phone the slider stops at its last card
- Given the shopper has opened the homepage on a phone
- When they press "Next" seven times
- Then the eighth card is in view
- And "Next" is disabled
Test: DcaShop.E2eTests.HomeSliderPhoneE2eTest#OnAPhoneTheSliderStopsAtItsLastCard

#### slider-does-not-move-by-itself
Title: The homepage slider does not move by itself
- Given the shopper has opened the homepage on a phone and the first card is in view
- When they wait 10 seconds without touching the slider
- Then the first card is still in view
Test: DcaShop.E2eTests.HomeSliderPhoneE2eTest#TheSliderDoesNotMoveByItself

### Rule: Every product has a page of its own, reached from its card

#### view-details-opens-the-product-page
Title: View Details on a catalogue card opens that product's page
- Given the seeded product "Domain-Driven Design"
- When a visitor follows the "View Details" link on its catalogue card
- Then the product page of "Domain-Driven Design" opens
- And its heading reads "Domain-Driven Design"
Test: DcaShop.E2eTests.ProductPageE2eTest#ViewDetailsOnACatalogueCardOpensThatProductsPage

### Rule: The page shows what the product is

#### product-page-shows-image-description-and-category
- Given the seeded product "Domain-Driven Design"
- When a visitor opens its product page
- Then the page shows the product's image, the description "The seminal work by Eric Evans that introduced the software industry to Domain-Driven Design. This essential guide teaches you how to tackle complexity in the heart of software by connecting implementation to an evolving model of the business domain."
- And it shows "Category" with the value "Books"
Test: DcaShop.IntegrationTests.ProductPageTest#TheProductPageShowsTheImageTheDescriptionAndTheCategory

### Rule: The page is recognisable as that product's page

#### product-page-title-is-the-product-name
- Given the seeded product "Clean Architecture"
- When a visitor opens its product page
- Then the browser tab title is "Clean Architecture"
Test: DcaShop.IntegrationTests.ProductPageTest#TheBrowserTabTitleIsTheProductName

#### product-page-breadcrumb
- Given the seeded product "Clean Architecture"
- When a visitor opens its product page
- Then the breadcrumb reads "Home / Products / Clean Architecture"
- And "Home" links to the home page and "Products" to the catalogue page
Test: DcaShop.IntegrationTests.ProductPageTest#TheBreadcrumbLeadsFromHomeOverProductsToTheProduct

### Rule: The visitor can always go back to the catalogue

#### back-to-products-returns-to-the-catalogue
- Given a visitor on the product page of "Team Topologies"
- When they follow "Back to Products"
- Then the catalogue page "Our Products" opens
Test: DcaShop.IntegrationTests.ProductPageTest#BackToProductsReturnsToTheCatalogue

### Rule: The shop starts with its sample catalogue

#### catalogue-lists-the-seeded-range
- Given the shop has started and seeded the sample catalogue of `seed-catalogue.json`
- When a visitor opens the catalogue page
- Then it shows 21 product cards
Test: DcaShop.E2eTests.SeededCatalogE2eTest#CatalogListsTheSeededProductsInNameOrder

### Rule: Products are listed by name, compared character by character

#### catalogue-is-in-name-order
- Given the seeded sample catalogue
- When a visitor opens the catalogue page
- Then the card titles are in ordinal order of the product name
- And the first card is `"Bounded Context" Enamel Pin`
Test: DcaShop.IntegrationTests.CatalogueTest#TheCardsAreInOrdinalOrderOfTheProductNameStartingWithTheEnamelPin

### Rule: A card shows what the product is

#### card-shows-name-description-and-image
- Given the seeded product "Domain-Driven Design"
- When a visitor opens the catalogue page
- Then its card shows the name "Domain-Driven Design", its description and its image
Test: DcaShop.IntegrationTests.CatalogueTest#TheCardOfDomainDrivenDesignShowsItsNameDescriptionAndImage

#### card-leads-to-the-product
- Given the seeded sample catalogue
- When a visitor opens the catalogue page
- Then every card offers a "View Details" link to that product's page
Test: DcaShop.IntegrationTests.CatalogueTest#EveryCardOffersViewDetailsLeadingToItsOwnProduct

### Rule: The page is recognisable as the catalogue

#### catalogue-page-heading
- Given the seeded sample catalogue
- When a visitor opens the catalogue page
- Then the page title is "Product Catalog", the heading reads "Our Products" and the breadcrumb reads "Home / Products"
Test: DcaShop.IntegrationTests.CatalogueTest#TheCatalogueIsTitledProductCatalogWithHeadingOurProductsBelowHomeProducts

### Rule: The catalogue page carries the catalogue's name as its title

#### catalogue-tab-reads-product-catalog
Title: The catalogue page is titled Product Catalog
- Given the seeded sample catalogue
- When a visitor opens the catalogue page
- Then the page title is "Product Catalog"
Test: DcaShop.E2eTests.CatalogTitleE2eTest#CatalogPageIsTitledProductCatalog

### Rule: A product address no product has shows the not-found page

#### unknown-product-shows-the-not-found-page
- Given the seeded sample catalogue, in which no product has the id "no-such-product"
- When a visitor opens the product page `/products/no-such-product`
- Then the page shows "404" and the heading "Page Not Found"
- And the message reads "The page you're looking for doesn't exist or has been removed. Perhaps you were looking for one of our products?"
Test: DcaShop.E2eTests.NotFoundTitleE2eTest#NotFoundPageIsTitledWithTheShopName

#### not-found-page-has-the-shop-title
- Given the seeded sample catalogue
- When a visitor opens the product page `/products/no-such-product`
- Then the browser tab title is "domaincentric.commerce"
Test: DcaShop.E2eTests.NotFoundTitleE2eTest#NotFoundPageIsTitledWithTheShopName

### Rule: The not-found page offers a way on

#### browse-all-products-leads-to-the-catalogue
- Given a visitor on the not-found page of `/products/no-such-product`
- When they follow "Browse All Products"
- Then the catalogue page "Our Products" opens
Test: DcaShop.IntegrationTests.NotFoundPageTest#BrowseAllProductsOnTheNotFoundPageOpensTheCatalogue

#### go-to-homepage-leads-to-the-home-page
- Given a visitor on the not-found page of `/products/no-such-product`
- When they follow "Go to Homepage"
- Then the home page "Welcome to domaincentric.commerce" opens
Test: DcaShop.IntegrationTests.NotFoundPageTest#GoToHomepageOnTheNotFoundPageOpensTheHomePage
