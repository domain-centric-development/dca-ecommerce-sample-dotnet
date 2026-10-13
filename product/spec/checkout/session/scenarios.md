# checkout — session

### Rule: A cart whose every line can be bought offers the checkout

#### cart-offers-proceed-to-checkout
- Given a guest whose cart holds "Domain-Driven Design" once
- When the guest opens the cart page
- Then it shows a "Proceed to Checkout" button
- And a "Continue Shopping" link to the catalogue page `/products`
Test: DcaShop.IntegrationTests.CartPageTest#ACartThatCanBeBoughtOffersTheCheckoutAndAWayBackToTheCatalogue

### Rule: Starting the checkout opens the buyer step

#### proceed-opens-the-buyer-step
- Given a guest whose cart holds "Domain-Driven Design" once
- When the guest presses "Proceed to Checkout" on the cart page
- Then the buyer step opens at `/checkout/buyer`
- And no login or registration was asked for
Test: DcaShop.E2eTests.StartCheckoutE2eTest#PressingProceedToCheckoutOpensTheBuyerStepWithoutAskingForAnAccount

### Rule: The checkout copies the cart's lines at today's catalogue price

#### buyer-step-summarises-the-cart
- Given a guest whose cart holds "Domain-Driven Design" twice and "Clean Architecture" once
- When the guest presses "Proceed to Checkout" on the cart page
- Then the buyer step shows an "Order Summary" with one entry per line, each with the product's image, its
  name, the quantity as "x<quantity>" and the line total
- And the entries read "Domain-Driven Design x2 109.98 EUR" and "Clean Architecture x1 39.99 EUR"
- And the summary's Subtotal reads "149.97 EUR"
Test: DcaShop.IntegrationTests.BuyerStepSummaryTest#TheBuyerStepSummarisesEachCartLineAndTheSubtotal
