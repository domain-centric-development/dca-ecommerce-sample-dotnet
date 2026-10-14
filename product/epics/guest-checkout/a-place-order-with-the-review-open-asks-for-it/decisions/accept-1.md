---
id: a-place-order-with-the-review-open-asks-for-it-accept-1
story: a-place-order-with-the-review-open-asks-for-it
stage: document
kind: acceptance
asked: 2026-10-14T16:55:13Z
digest: c8fb4c8257575d90266de0487d3020ddada7f1aab8a8d21bd96670bb61506de4
---

# Accept a-place-order-with-the-review-open-asks-for-it?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- place-order-with-the-review-open-asks-for-the-review: Given a guest whose review step is open to them, but who has not placed the order from it; When a "Place Order" is sent to the shop from another step page than the review; Then the guest ends on the review step at `/checkout/review`; And the page says "Please review your order before placing it."; And no confirmation is shown: the order is not placed — `DcaShop.E2eTests.PlaceOrderWithTheReviewOpenE2eTest#APlaceOrderSentFromThePaymentStepEndsOnTheReviewWhichAsksToReviewTheOrder`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-14T17:05:45Z
