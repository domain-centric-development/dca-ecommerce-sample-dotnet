---
id: early-step-leads-to-the-open-step-accept-1
story: early-step-leads-to-the-open-step
stage: document
kind: acceptance
asked: 2026-10-13T11:37:12Z
digest: 075e93cb02c41e0aeec005c1edbc05d137134b2c568ffa2b1a1b0d8ca956257b
---

# Accept early-step-leads-to-the-open-step?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- delivery-sent-before-the-buyer-step: Given a guest who has just started the checkout and has not submitted the buyer step; When a delivery form with Street Address "Hauptstrasse 1", City "Berlin", Postal Code "10115", Country; Then the guest ends on the buyer step at `/checkout/buyer`; And the review cannot be opened afterwards: `/checkout/review` opens the buyer step — `DcaShop.E2eTests.StepSentTooEarlyE2eTest#ADeliveryFormSentBeforeTheBuyerStepEndsOnTheBuyerStepAndKeepsTheReviewClosed`
- place-order-before-the-review: Given a guest who has submitted the buyer and delivery steps and is on the payment step; When a "Place Order" is sent to the shop; Then the guest ends on the payment step at `/checkout/payment`; And no confirmation is shown: the order is not placed — `DcaShop.IntegrationTests.StepSentTooEarlyTest#PlaceOrderSentFromThePaymentStepLeadsToThePaymentStepAndPlacesNoOrder`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-13T13:00:34Z
