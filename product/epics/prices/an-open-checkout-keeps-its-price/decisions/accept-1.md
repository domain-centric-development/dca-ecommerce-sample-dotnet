---
id: an-open-checkout-keeps-its-price-accept-1
story: an-open-checkout-keeps-its-price
stage: document
kind: acceptance
asked: 2026-10-21T11:34:57Z
digest: 01522cc46f75ecddbf19a8d02cf949922daf8b5f5ac12b4c770d66c5170ac84c
---

# Accept an-open-checkout-keeps-its-price?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- the-review-opened-again-shows-todays-price: Given a guest at the review with "Domain-Driven Design" once and "Standard Shipping", shown at 54.99 EUR; And the price of "Domain-Driven Design" has since been set to 49.99 EUR through the staff API; When the guest opens the review again; Then the review shows "Domain-Driven Design x1 49.99 EUR" and Total "54.98 EUR" — `DcaShop.IntegrationTests.ReviewFingerprintTest#OpeningTheReviewLaterShowsTodaysPriceAndTotalAndStoresNothing`
- place-order-after-a-price-change-since-the-review-is-refused: Given a guest at the review with "Domain-Driven Design" once and "Standard Shipping", shown at 54.99 EUR; And the price of "Domain-Driven Design" has since been set to 49.99 EUR through the staff API; When the guest presses "Place Order"; Then the review stays open at `/checkout/review` with "The price of 'Domain-Driven Design' has changed. Please check your order and place it again."; And it shows "Domain-Driven Design x1 49.99 EUR" and Total "54.98 EUR"; And no confirmation is shown — `DcaShop.E2eTests.PlaceOrderAtTheReviewsPriceE2eTest#APriceChangedWhileReviewingRefusesTheOrderAndShowsTodaysPrice`
- place-order-after-a-change-in-another-tab-is-refused: Given a guest at the review with "Domain-Driven Design" once and "Standard Shipping", shown at 54.99 EUR; And in a second tab of the same browser the guest has since chosen "Express Shipping" on the delivery step; When the guest presses "Place Order" on the first tab's review; Then the review stays open at `/checkout/review` with "Your order has changed since you reviewed it. Please check it and place it again."; And it shows "Express Shipping"; And no confirmation is shown — `DcaShop.IntegrationTests.OrderChangedInAnotherTabPageTest#AShippingChosenInAnotherTabRefusesTheReviewedOrder`
- place-order-after-the-refusal-orders-what-the-review-now-shows: Given a guest whose "Place Order" was refused because the price of "Domain-Driven Design" changed from 54.99 EUR to 49.99 EUR, and the review now shows 49.99 EUR; When the guest presses "Place Order" again; Then the confirmation page shows under "Items Ordered" "Domain-Driven Design x1 49.99 EUR" — `DcaShop.IntegrationTests.ReviewFingerprintTest#AfterAPriceRefusalTheOrderIsPlacedAtThePriceNowShown`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: correction: none of a/b/c — the payment is authorized at "Place Order", after the fingerprint and the stock are checked, for exactly the total the review showed; the payment step only chooses how to pay. The confirmation shows the prices of the moment the order was placed and nothing changes them afterwards. Written as its own story, `the-payment-is-authorized-at-place-order` (epic provider-payment), which this story now depends on; this story gains: a refused "Place Order" requests no payment, and the order after the refusal is authorized at the total then shown.
by: Christoph Bloemer
at: 2026-10-21T14:32:51Z
rationale: the payment is authorized on the payment step for the total of that moment; once the review shows today's prices, Place Order confirms an order the authorization does not cover (found by the Java shop's judge, decision an-open-checkout-keeps-its-price-03).
