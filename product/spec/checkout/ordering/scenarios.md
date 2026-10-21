# checkout — ordering

### Rule: The review shows what the guest entered

#### review-shows-buyer-delivery-and-payment
- Given the guest has chosen the payment provider
- When the guest opens the review step
- Then the section "Buyer Information" shows "Jane Doe", "jane@example.com" and "+49 30 1234567"
- And the section "Delivery" shows under "Shipping Address" "Hauptstrasse 1", "Berlin, 10115" and "Germany",
  and under "Shipping Method" "Standard Shipping", "Estimated Delivery: 5-7 business days" and
  "Cost: 4.99 EUR"
- And the section "Payment Method" names the chosen payment provider
Test: DcaShop.IntegrationTests.ReviewAndConfirmationTest#TheReviewShowsTheBuyerTheDeliveryAndTheChosenProvider

### Rule: The review shows the items and the totals, with the 19 % VAT contained in the total

#### review-shows-items-and-totals
- Given the guest has chosen the payment provider
- When the guest opens the review step
- Then "Order Items" shows "Domain-Driven Design x1 54.99 EUR"
- And the totals read Subtotal "54.99 EUR", Shipping "4.99 EUR", Tax "9.58 EUR" and Total "59.98 EUR"
- And a "Place Order" button
Test: DcaShop.IntegrationTests.ReviewAndConfirmationTest#TheReviewShowsTheItemsAndTheTotalsWithTheContainedTax

### Rule: Placing the order confirms it and opens the confirmation

#### guest-places-the-order
- Given the guest is on the review step
- When the guest presses "Place Order"
- Then the confirmation page opens at `/checkout/confirmation`
- And it reads "Your order has been placed successfully"
Test: DcaShop.E2eTests.PlaceTheOrderE2eTest#PlacingTheOrderOpensTheConfirmation

#### confirmation-shows-what-was-ordered
- Given the guest is on the review step
- When the guest presses "Place Order"
- Then the confirmation page shows under "Contact Information" "Jane Doe", "jane@example.com" and
  "+49 30 1234567"
- And under "Delivery Address" "Hauptstrasse 1", "Berlin, 10115" and "Germany"
- And under "Shipping Method" "Standard Shipping" and "Estimated Delivery: 5-7 business days"
- And under "Payment Method" the chosen payment provider
- And under "Items Ordered" "Domain-Driven Design x1 54.99 EUR"
- And the amounts Subtotal "54.99 EUR", Shipping "4.99 EUR" and Tax "9.58 EUR"
Test: DcaShop.IntegrationTests.ReviewAndConfirmationTest#TheConfirmationRepeatsTheContactTheDeliveryThePaymentTheItemsAndTheAmounts

#### continue-shopping-leads-home
- Given the guest has placed the order and sees the confirmation page
- When the guest follows "Continue Shopping"
- Then the home page `/` opens
Test: DcaShop.IntegrationTests.ReviewAndConfirmationTest#ContinueShoppingOnTheConfirmationLeadsToTheHomePage

### Rule: A cart with a line short of stock cannot start the checkout

#### cart-shows-cannot-checkout-when-stock-ran-short
- Given a guest whose cart holds "Wooden Hexagon Desk Model" five times
- And another visitor has since bought it five times, leaving 3
- When the guest opens the cart page
- Then instead of "Proceed to Checkout" it shows a disabled "Cannot Checkout" button
- And the note "Some items are unavailable or have insufficient stock"
Test: DcaShop.IntegrationTests.CheckoutStartFromTheCartTest#ACartShortOfStockOffersNoCheckoutAndSaysWhy

#### start-from-an-outdated-cart-page-is-refused
- Given a guest whose cart page, opened while it held "Wooden Hexagon Desk Model" five times with 8 in stock,
  still shows "Proceed to Checkout"
- And another visitor has since bought it five times, leaving 3
- When the guest presses "Proceed to Checkout" on that page
- Then the cart page opens with the error "Cannot start checkout, 1 item(s) unavailable or out of stock"
Test: DcaShop.IntegrationTests.OutdatedCartStartTest#StartingFromACartWhoseLineRanShortOfStockIsRefusedNamingThatLine

#### start-from-an-emptied-cart-is-refused
- Given a guest whose cart page, opened while it held "Domain-Driven Design" once, still shows
  "Proceed to Checkout"
- And the guest has since placed an order for exactly that cart in another tab, and the cart is now empty
- When the guest presses "Proceed to Checkout" on the old page
- Then the cart page opens with the error "Cannot check out an empty cart."
Test: DcaShop.IntegrationTests.CheckoutStartFromTheCartTest#StartingFromACartEmptiedByTheOrderIsRefused

### Rule: Place Order checks every line again and changes nothing when one fails

#### place-order-is-refused-when-stock-ran-short
- Given a guest at the review with "Wooden Hexagon Desk Model" five times
- And another visitor has since bought it five times, leaving 3
- When the guest presses "Place Order"
- Then the review step stays open at `/checkout/review` and shows
  "Not enough stock left for 'Wooden Hexagon Desk Model'. Please adjust your order."
- And no confirmation is shown
- And the product page still reads "3 units available" and the guest's cart still holds the product five
  times
Test: DcaShop.IntegrationTests.PlaceOrderShortOfStockPageTest#APlaceOrderForMoreThanTheStockLeftNamesTheProductAndChangesNothing

#### place-order-is-refused-when-sold-out
- Given a guest at the review with "Wooden Hexagon Desk Model" twice
- And another visitor has since bought all 8
- When the guest presses "Place Order"
- Then the review step stays open at `/checkout/review` and shows
  "Not enough stock left for 'Wooden Hexagon Desk Model'. Please adjust your order."
- And no confirmation is shown
Test: DcaShop.IntegrationTests.PlaceOrderRefusalPageTest#ARefusedPlaceOrderOpensTheReviewAgainWithItsError

### Rule: The confirmation promises no email and no order tracking

#### the-confirmation-promises-no-email
- Given the guest "jane@example.com" is on the review step
- When the guest presses "Place Order"
- Then the confirmation page does not say "We've sent a confirmation email"
- And it does not say "You can track your order status using your order reference."
Test: DcaShop.E2eTests.PlaceTheOrderE2eTest#TheConfirmationPromisesNeitherAnEmailNorOrderTracking

### Rule: The confirmation calls the amount the order's total, not an amount paid

#### the-confirmation-shows-the-order-total
- Given the guest is on the review step with "Domain-Driven Design" once and "Standard Shipping"
- When the guest presses "Place Order"
- Then the confirmation page shows "Order Total" "59.98 EUR"
- And it does not say "Total Paid"
Test: DcaShop.IntegrationTests.ConfirmationOrderTotalTest#TheConfirmationCallsTheAmountTheOrderTotal

### Rule: Of orders placed at the same time, only those the stock still covers are placed

#### the-order-the-stock-no-longer-covers-is-refused
- Given the seeded "Wooden Hexagon Desk Model" with a stock of 8
- And two guests, each at the review with "Wooden Hexagon Desk Model" five times
- When both press "Place Order" at the same moment
- Then exactly one of them sees the confirmation
- And the other is back at the review `/checkout/review` with
  "Not enough stock left for 'Wooden Hexagon Desk Model'. Please adjust your order."
- And shortly afterwards the product page of "Wooden Hexagon Desk Model" reads "3 units available"
Test: DcaShop.E2eTests.TwoOrdersAtOnceE2eTest#OfTwoOrdersPlacedAtOnceOnlyTheOneTheStockCoversIsPlaced

### Rule: The review shows today's prices whenever it is opened

#### the-review-opened-again-shows-todays-price
- Given a guest at the review with "Domain-Driven Design" once and "Standard Shipping", shown at 54.99 EUR
- And the price of "Domain-Driven Design" has since been set to 49.99 EUR through the staff API
- When the guest opens the review again
- Then the review shows "Domain-Driven Design x1 49.99 EUR" and Total "54.98 EUR"
Test: DcaShop.IntegrationTests.ReviewFingerprintTest#OpeningTheReviewLaterShowsTodaysPriceAndTotalAndStoresNothing

### Rule: Place Order orders only what the review showed; anything changed since refuses it and shows today's order

#### place-order-after-a-price-change-since-the-review-is-refused
Title: Place Order after a price change since the review is refused and the review shows today's price
- Given a guest at the review with "Domain-Driven Design" once and "Standard Shipping", shown at 54.99 EUR
- And the price of "Domain-Driven Design" has since been set to 49.99 EUR through the staff API
- When the guest presses "Place Order"
- Then the review stays open at `/checkout/review` with "The price of 'Domain-Driven Design' has changed. Please check
  your order and place it again."
- And it shows "Domain-Driven Design x1 49.99 EUR" and Total "54.98 EUR"
- And no confirmation is shown
- And the payment provider received no payment request
Test: DcaShop.E2eTests.PlaceOrderAtTheReviewsPriceE2eTest#APriceChangedWhileReviewingRefusesTheOrderAndShowsTodaysPrice

#### place-order-after-a-change-in-another-tab-is-refused
- Given a guest at the review with "Domain-Driven Design" once and "Standard Shipping", shown at 54.99 EUR
- And in a second tab of the same browser the guest has since chosen "Express Shipping" on the delivery step
- When the guest presses "Place Order" on the first tab's review
- Then the review stays open at `/checkout/review` with "Your order has changed since you reviewed it. Please check it
  and place it again."
- And it shows "Express Shipping"
- And no confirmation is shown
Test: DcaShop.IntegrationTests.OrderChangedInAnotherTabPageTest#AShippingChosenInAnotherTabRefusesTheReviewedOrder

#### place-order-after-the-refusal-orders-what-the-review-now-shows
- Given a guest whose "Place Order" was refused because the price of "Domain-Driven Design" changed from 54.99 EUR to
  49.99 EUR, and the review now shows 49.99 EUR
- When the guest presses "Place Order" again
- Then the confirmation page shows under "Items Ordered" "Domain-Driven Design x1 49.99 EUR"
- And the payment provider received one payment request, of "54.98"
Test: DcaShop.IntegrationTests.ReviewFingerprintTest#AfterAPriceRefusalTheOrderIsPlacedAtThePriceNowShown
