---
id: checkout-refusals-say-what-happened-accept-1
story: checkout-refusals-say-what-happened
stage: document
kind: acceptance
asked: 2026-11-11T00:57:10Z
digest: e2c55cced156f5558548122918ab61d3cf1ffe9c46492dfc5499d85d16dbc55b
---

# Accept checkout-refusals-say-what-happened?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- a-changed-quantity-is-not-named-a-price-change: Given a customer on the review of "Clean Architecture" once, at 39.99 EUR; And in another tab the customer changed the cart's quantity of "Clean Architecture" to 2 and went through the checkout to the review again; When the customer presses "Place Order" on the first tab's review; Then the review stays open at `/checkout/review` with "Your order has changed since you reviewed it. Please check it and place it again."; And it does not say that a price changed — `DcaShop.E2eTests.OrderChangedInAnotherTabE2eTest#AQuantityChangedInAnotherTabIsNamedAChangedOrderNotAPrice`
- a-withdrawn-way-to-pay-opens-the-payment-step: Given a customer on the review who chose "Mock Payment (Test)"; And the shop no longer offers "Mock Payment (Test)"; When the customer presses "Place Order"; Then the payment step `/checkout/payment` opens with "This way to pay is not offered. Please choose another one."; And no order is placed — `DcaShop.IntegrationTests.WithdrawnWayToPayPageTest#AWayToPayNoLongerOfferedOpensThePaymentStepAndPlacesNoOrder`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.
