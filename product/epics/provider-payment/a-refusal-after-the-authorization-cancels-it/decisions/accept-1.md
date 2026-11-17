---
id: a-refusal-after-the-authorization-cancels-it-accept-1
story: a-refusal-after-the-authorization-cancels-it
stage: document
kind: acceptance
asked: 2026-11-11T00:26:41Z
digest: 38928ccc0e8acbd058705a7054dacb68541da0834809f975260d7c622d83ab81
---

# Accept a-refusal-after-the-authorization-cancels-it?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- a-refused-order-cancels-its-authorization: Given a customer on the review of "Clean Architecture" at 39.99 EUR, paying with a provider that authorizes payments; And while the provider authorizes the 39.99 EUR, the checkout is replaced by a new checkout from the same cart; When the customer presses "Place Order" on the first checkout's review; Then the order is not placed and the review stays open with the refusal it shows today; And the provider receives `POST /payments/{payment reference}/cancel` for the payment it authorized — `DcaShop.E2eTests.ProviderPaymentE2eTest#AnOrderRefusedAfterTheProviderAuthorizedItHasTheAuthorizationCancelled`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.
