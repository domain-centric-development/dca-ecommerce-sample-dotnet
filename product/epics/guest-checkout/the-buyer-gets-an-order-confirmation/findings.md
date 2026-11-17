---
story: the-buyer-gets-an-order-confirmation
epic: guest-checkout
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (5 rows, 0 without a fitting value)
---

# Findings — the-buyer-gets-an-order-confirmation

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Checkout/Application/OrderConfirmation/SendOrderConfirmation/SendOrderConfirmationUseCase.cs:40 | minor | introduced |  | A confirmed order without a buyer fails with an unnamed error that would be retried forever. | domain-concept: unreachable guard (`Confirm` requires the buyer step, `CheckoutSession.cs:112`) raised as a platform exception. | Let `CheckoutSession` answer the recipient, or raise a named failure. | done | code | domain-concept | incorrect |
| 2 | hexagonal | src/DcaShop.Checkout/Application/OrderConfirmation/SendOrderConfirmation/SendOrderConfirmationUseCase.cs:48 | minor | introduced |  | The step records the sent mail outside the transaction the other event reactions use. | `ContainsAsync`/`MarkProcessedAsync` run without `ITransactionBoundary`, unlike Cart's idempotent consumer. | Wrap `MarkProcessedAsync` in `InTransactionAsync`, the send outside it. | done | code | timing | missing |
| 3 | clean-code | src/DcaShop.Checkout/Adapter/Outgoing/Persistence/InMemoryProcessedEvents.cs:9 | minor | introduced |  | A comment points to a decision that says nothing about this store. | Cites ADR-001 (solution layout). | Drop the citation. | done | code | comment | extraneous |
| 4 | clean-code | src/DcaShop.Checkout/Application/OrderConfirmation/SendOrderConfirmation/SendOrderConfirmationUseCase.cs:42 | minor | introduced |  | The mail's resend key is built inline, its stability unstated. | `$"order-confirmation-{sessionId}"` has no name, unlike `Effect` (:14). | Extract a named key with a stability note. | done | code | naming | missing |
| 5 | clean-code | src/DcaShop.Checkout/Application/Shared/IOrderConfirmationMailPort.cs:17 | minor | introduced |  | The mail step answers yes or no without saying what no means. | `Task<bool> SendAsync` reads as "if not send" at the call site. | Rename to `TrySendAsync` or return a named answer. | done | code | interface | incorrect |
