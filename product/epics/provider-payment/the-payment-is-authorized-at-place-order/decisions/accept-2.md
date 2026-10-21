---
id: the-payment-is-authorized-at-place-order-accept-2
story: the-payment-is-authorized-at-place-order
stage: document
kind: acceptance
asked: 2026-10-21T16:51:58Z
digest: 4b8af42f61754d7ccaa59d28f9fbb175a1f86fbec641fc513ac7fe1d62151b76
---

# Accept the-payment-is-authorized-at-place-order?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- authorized-payment-moves-to-review: Given a checkout session at the payment step; And the payment provider authorizes payments; When the customer chooses "Mock Payment (Test)" and presses "Continue to Review"; Then the checkout shows the review step; And the payment provider received no payment request — `DcaShop.IntegrationTests.PlaceOrderPaymentTest#ChoosingHowToPayOpensTheReviewWithoutAPaymentRequest`
- place-order-authorizes-the-reviewed-total: Given a guest at the review with "Domain-Driven Design" once and "Standard Shipping", Total "59.98 EUR"; And the payment provider authorizes payments; When the guest presses "Place Order"; Then the confirmation page opens at `/checkout/confirmation` with "Order Total" "59.98 EUR"; And the payment provider received one payment request, of "59.98" — `DcaShop.E2eTests.ProviderPaymentE2eTest#PlaceOrderHasTheProviderAuthorizeTheReviewedTotalAndOpensTheConfirmation`
- the-payment-request-names-its-checkout: Given a checkout session at the review; And the payment provider authorizes payments; When the customer presses "Place Order"; Then the payment provider received one payment request for the checkout session's total; And that request carries the checkout reference, the checkout session's id as text, as `merchantReference` — `DcaShop.IntegrationTests.PaymentReferenceTest#PlaceOrderSendsOnePaymentRequestForTheSessionTotalNamingItsCheckout`
- two-checkouts-send-two-references: Given two customers, each with a checkout session at the review; When both press "Place Order"; Then the payment provider received two payment requests with two different `merchantReference` values — `DcaShop.IntegrationTests.PaymentReferenceTest#TwoCheckoutsSendTwoDifferentReferences`
- refused-payment-stays-at-payment: Given a checkout session at the review; And the payment provider refuses payments; When the customer presses "Place Order"; Then the review stays open at `/checkout/review` with "The payment was refused. Please choose another way to pay."; And no confirmation is shown — `DcaShop.IntegrationTests.PlaceOrderPaymentRefusalPageTest#ARefusedPaymentKeepsTheCustomerAtTheReviewWithTheRefusalText, DcaShop.IntegrationTests.PlaceOrderPaymentTest#ARefusedPaymentAtPlaceOrderHoldsNoStock`
- a-refused-payment-holds-no-stock: Given a guest at the review with "Wooden Hexagon Desk Model" twice, whose product page reads "8 units available"; And the payment provider refuses payments; When the guest presses "Place Order"; Then the review stays open at `/checkout/review` with "The payment was refused. Please choose another way to pay."; And the product page of "Wooden Hexagon Desk Model" still reads "8 units available" — `DcaShop.IntegrationTests.PlaceOrderPaymentTest#ARefusedPaymentAtPlaceOrderHoldsNoStock, DcaShop.IntegrationTests.PlaceOrderPaymentRefusalPageTest#ARefusedPaymentKeepsTheCustomerAtTheReviewWithTheRefusalText`
- the-provider-answers-per-checkout: Given two customers, each with a checkout session at the review; And the payment provider refuses the payment for the first customer's checkout reference only; When both press "Place Order"; Then the first customer stays at the review with "The payment was refused. Please choose another way to pay."; And the second customer sees the confirmation — `DcaShop.IntegrationTests.PaymentReferenceTest#TheProviderRefusingOneCheckoutLeavesTheOtherCheckoutConfirmed`
- a-second-attempt-names-the-same-checkout: Given a checkout session at the review; And the customer's first "Place Order" was refused by the payment provider; When the customer presses "Place Order" again; Then the payment provider received two payment requests for that checkout session; And both carry the same `merchantReference` — `DcaShop.IntegrationTests.PaymentReferenceTest#ASecondPlaceOrderAfterARefusalNamesTheSameCheckout`
- slow-provider-counts-as-unavailable: Given a checkout session at the review; And the payment provider answers only after 5 seconds; When the customer presses "Place Order"; Then the review stays open at `/checkout/review` with "The payment provider is not available right now. Please try again later."; And no confirmation is shown — `DcaShop.IntegrationTests.PlaceOrderPaymentTest#AProviderThatAnswersPlaceOrderOnlyAfterFiveSecondsCountsAsUnavailable, DcaShop.IntegrationTests.PlaceOrderPaymentRefusalPageTest#AnUnavailableProviderKeepsTheCustomerAtTheReviewWithTheUnavailableText`
- place-order-refused-for-stock-requests-no-payment: Given a guest at the review with "Wooden Hexagon Desk Model" five times; And another visitor has since bought it five times, leaving 3; When the guest presses "Place Order"; Then the review stays open at `/checkout/review` with "Not enough stock left for 'Wooden Hexagon Desk Model'. Please adjust your order."; And the payment provider received no payment request — `DcaShop.IntegrationTests.PlaceOrderPaymentTest#APlaceOrderRefusedForTheStockRequestsNoPayment`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-21T17:51:05Z
