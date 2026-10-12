# cart — shopping

### Rule: "Add to Cart" puts one unit of the product into the visitor's cart and opens the cart page

#### add-to-cart-lands-on-the-cart
- Given a guest with an empty cart on the product page of "Domain-Driven Design"
- When they press "Add to Cart"
- Then they are on the cart page at `/cart`
- And the page shows the message "Product added to cart!"
- And the cart shows one line "Domain-Driven Design" with quantity 1
Test: DcaShop.E2eTests.AddToCartE2eTest#AddToCartLandsOnTheCart

#### cart-page-heading
- Given a guest who added "Domain-Driven Design" to the cart
- When they open `/cart`
- Then the page title and the heading read "Shopping Cart"
Test: DcaShop.IntegrationTests.CartPageTest#TheCartPageIsTitledAndHeadedShoppingCart

### Rule: A product appears on one line only; adding it again raises that line's quantity

#### adding-again-raises-the-quantity
- Given a guest whose cart holds one line "Domain-Driven Design" with quantity 1
- When they press "Add to Cart" on the product page of "Domain-Driven Design" again
- Then the cart shows one line "Domain-Driven Design" with quantity 2
- And no second line for "Domain-Driven Design"
Test: DcaShop.IntegrationTests.AddItemToCartTest#AddingTheSameProductAgainRaisesTheQuantityOfItsLine

#### another-product-gets-its-own-line
- Given a guest whose cart holds one line "Domain-Driven Design" with quantity 1
- When they press "Add to Cart" on the product page of "Clean Architecture"
- Then the cart shows two lines: "Domain-Driven Design" with quantity 1 and "Clean Architecture" with quantity 1
Test: DcaShop.IntegrationTests.AddItemToCartTest#AnotherProductGetsALineOfItsOwn
