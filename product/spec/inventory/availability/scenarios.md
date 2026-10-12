# inventory — availability

### Rule: A product starts with the stock it was created with

#### product-page-shows-units-available
- Given the seeded product "Domain-Driven Design", created with a stock of 20
- When a visitor opens its product page
- Then the page shows the badge "In Stock"
- And it shows "Availability" with the value "20 units available"
Test: DcaShop.E2eTests.ProductAvailabilityE2eTest#ProductPageShowsUnitsAvailable

#### few-units-are-counted-exactly
- Given the seeded product "Wooden Hexagon Desk Model", created with a stock of 8
- When a visitor opens its product page
- Then it shows "Availability" with the value "8 units available"
Test: DcaShop.IntegrationTests.ProductAvailabilityTest#AProductWithFewUnitsShowsExactlyHowManyAreLeft

### Rule: A product with stock is shown as in stock on its card

#### card-shows-in-stock
- Given the seeded sample catalogue, in which every product has stock
- When a visitor opens the catalogue page
- Then every card shows the badge "In Stock"
Test: DcaShop.IntegrationTests.ProductAvailabilityTest#EveryCardOfTheSeededCatalogueShowsInStock

### Rule: "Add to Cart" is offered only for an available product

#### available-product-offers-add-to-cart
- Given the seeded product "Clean Architecture", created with a stock of 35
- When a visitor opens its product page
- Then the page offers an "Add to Cart" button next to "Back to Products"
Test: DcaShop.IntegrationTests.ProductAvailabilityTest#AnAvailableProductOffersAddToCartNextToBackToProducts

### Rule: A product with no stock left is shown as out of stock and cannot be added

#### card-shows-out-of-stock
- Given the product "Wooden Hexagon Desk Model" has 0 units in stock
- When a visitor opens the catalogue page
- Then its card shows the badge "Out of Stock"
Test: DcaShop.IntegrationTests.OutOfStockProductTest#TheCardOfAProductWithNoStockShowsOutOfStock

#### out-of-stock-product-page
- Given the product "Wooden Hexagon Desk Model" has 0 units in stock
- When a visitor opens its product page
- Then the page shows the badge "Out of Stock" and "Availability" with the value "Currently out of stock"
- And it offers no "Add to Cart" button, only "Back to Products"
Test: DcaShop.IntegrationTests.OutOfStockProductTest#TheProductPageOfAProductWithNoStockShowsItOutOfStockAndOffersOnlyTheWayBack
