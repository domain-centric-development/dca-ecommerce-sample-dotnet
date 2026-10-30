---
story: the-order-confirmation-findings-are-closed
epic: guest-checkout
---

# Findings — the-order-confirmation-findings-are-closed

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Checkout/Application/OrderConfirmation/SendOrderConfirmation/SendOrderConfirmationUseCase.cs:53 | minor | The mail step refuses on "anything but accepted" while calling it "unavailable". | `is not MailOutcome.Accepted` names the complement of the happy path, not the outcome the thrown `MailProviderUnavailableException` names; the payment twin tests `== PaymentOutcome.Unavailable` (`ConfirmCheckoutUseCase.cs:111`). | `if (outcome == IOrderConfirmationMailer.MailOutcome.Unavailable)`. | open |
| 2 | clean-code | src/DcaShop.Checkout/Application/OrderConfirmation/SendOrderConfirmation/SendOrderConfirmationUseCase.cs:51 | minor | The same rule about sending the mail first is written twice in one file. | The line comment restates the class summary (`:11-13`), which also carries the ADR citation; two statements drift. | Drop the line comment or shorten it to "mail first, then the transaction". | open |
| 3 | plan | src/DcaShop.Cart/Adapter/Outgoing/Persistence/InMemoryProcessedEvents.cs:9 | minor | Three other contexts' effect stores still cite a decision that is not about them. | deferred by the plan: the Cart, Inventory and Pricing twins (same path, `:9`) carry the ADR-001 citation row #3 dropped from Checkout's. | Drop the citation in all three. | open |
