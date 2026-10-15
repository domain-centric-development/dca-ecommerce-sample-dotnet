---
story: paying-again-requests-no-second-payment
epic: provider-payment
---

# Findings — paying-again-requests-no-second-payment

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | plan | src/DcaShop.Checkout/Domain/Model/CheckoutSession.cs:135 | minor | Cancelling a payment sends the checkout back to the payment step, but no unit test checks that. | `ReleasePayment` adds two rules the plan's `## Invariants` does not name (no selection left; review falls back to the payment step); build.md says no unit test names them. | Add a `CheckoutSessionTest`: a session at the review, after `ReleasePayment`, has no `PaymentSelection`, `CurrentStep` is `Payment`, `IsTotalCoveredBy` is false. | open |
| 2 | ddd | src/DcaShop.Checkout/Domain/Model/CheckoutSession.cs:135 | minor | The story calls it "cancelling the payment", but the code uses three different words for it. | The port says `CancelPaymentAsync`, the aggregate `ReleasePayment`, the use case "release" and "forget" (`SubmitPaymentUseCase.cs:138`). | Pick one word. Either use the story's "cancel", or put "release" in the glossary as the session's side of a cancellation and drop "forget". | open |
| 3 | clean-code | src/DcaShop.Checkout/Application/CheckoutCompletion/SubmitPayment/SubmitPaymentUseCase.cs:158 | minor | A comment gives a reason that applies to only one of the two cases where the code runs. | The token comment justifies itself with "the transaction failed"; the new caller (`ReleaseAsync`, l.134) runs before any transaction. | Reword it for both callers: a clean-up must reach the provider even when the caller's request was cancelled. | open |
