---
story: the-buyer-gets-an-order-confirmation
epic: guest-checkout
---

# Findings — the-buyer-gets-an-order-confirmation

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Checkout/Application/OrderConfirmation/SendOrderConfirmation/SendOrderConfirmationUseCase.cs:40 | minor | A confirmed order without a buyer fails with an unnamed error that would be retried forever. | domain-concept: unreachable guard (`Confirm` requires the buyer step, `CheckoutSession.cs:112`) raised as a platform exception. | Let `CheckoutSession` answer the recipient, or raise a named failure. | open |
| 2 | hexagonal | src/DcaShop.Checkout/Application/OrderConfirmation/SendOrderConfirmation/SendOrderConfirmationUseCase.cs:48 | minor | The step records the sent mail outside the transaction the other event reactions use. | `ContainsAsync`/`MarkProcessedAsync` run without `ITransactionBoundary`, unlike Cart's idempotent consumer. | Wrap `MarkProcessedAsync` in `InTransactionAsync`, the send outside it. | open |
| 3 | clean-code | src/DcaShop.Checkout/Adapter/Outgoing/Persistence/InMemoryProcessedEvents.cs:9 | minor | A comment points to a decision that says nothing about this store. | Cites ADR-001 (solution layout). | Drop the citation. | open |
| 4 | clean-code | src/DcaShop.Checkout/Application/OrderConfirmation/SendOrderConfirmation/SendOrderConfirmationUseCase.cs:42 | minor | The mail's resend key is built inline, its stability unstated. | `$"order-confirmation-{sessionId}"` has no name, unlike `Effect` (:14). | Extract a named key with a stability note. | open |
| 5 | clean-code | src/DcaShop.Checkout/Application/Shared/IOrderConfirmationMailPort.cs:17 | minor | The mail step answers yes or no without saying what no means. | `Task<bool> SendAsync` reads as "if not send" at the call site. | Rename to `TrySendAsync` or return a named answer. | open |
