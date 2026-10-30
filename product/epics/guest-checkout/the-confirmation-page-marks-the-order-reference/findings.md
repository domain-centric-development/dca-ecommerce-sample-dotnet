---
story: the-confirmation-page-marks-the-order-reference
epic: guest-checkout
---

# Findings — the-confirmation-page-marks-the-order-reference

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | ddd | `src/DcaShop.Web/Views/Checkout/Confirmation.cshtml:8` | minor | The confirmation page calls the checkout's own number the order reference, while the checkout's glossary says the order reference is something else. | domain-term: the hook `order-reference` and the label "Order Reference:" name `@s.SessionId`, the checkout session's id; the Checkout glossary says `Complete` records the order reference (`product/spec/checkout/glossary.md:77`) and that the session id as text "is not … the order reference" (`product/spec/checkout/glossary.md:660-663`); the page object's `OrderReferenceAsync` (`tests/DcaShop.E2eTests/Pages/ConfirmationPage.cs:46`) carries the synonym into the test language. The label and the value predate the story and the hook name is the story's own, shared with the Java shop, so this does not block; confirmed as the state the change rests on. | A language decision for both shops, not this story: either the confirmation shows the order reference `Complete` records once something sets it, or the glossary states that the reference a buyer sees on the confirmation is the checkout session's id and its "Checkout reference" entry drops "and not the order reference" — `/ubiquitous-language` on *Order reference* in Checkout. | open |
