---
id: paying-again-requests-no-second-payment-accept-1
story: paying-again-requests-no-second-payment
stage: document
kind: acceptance
asked: 2026-10-15T10:14:21Z
digest: 23a3c6aafe613378b0623fb2d35e314b4ef894b9daf40f2e440c3defe12cf118
---

# Accept paying-again-requests-no-second-payment?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- paying-again-for-the-same-amount-keeps-the-payment: Given a checkout session whose payment of its total the payment provider authorized; And the customer went back from the review to the payment step; When the customer pays through the payment provider again; Then the checkout shows the review step and the message "Your payment is already authorized."; And the payment provider received one payment request for that checkout session — `DcaShop.E2eTests.PayingAgainE2eTest#PayingAgainForTheSameAmountKeepsTheAuthorizedPayment`
- paying-again-for-a-changed-amount-replaces-the-payment: Given a checkout session with "Domain-Driven Design" once and "Standard Shipping", whose payment of "59.98" the; And the customer went back from the review to the delivery step and chose "Express Shipping"; When the customer pays through the payment provider again; Then the payment provider received the cancellation of the first payment; And a second payment request of "64.98" with the same `merchantReference` as the first; And the checkout shows the review step — `DcaShop.IntegrationTests.PayingAgainTest#PayingAgainForAChangedTotalCancelsTheFirstPaymentAndRequestsTheNewTotal`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-15T14:56:00Z
rationale: Accepted by the person (2026-10-15), recorded by the managing session.
