---
story: a-place-order-with-the-review-open-asks-for-it
epic: guest-checkout
---

# Findings — a-place-order-with-the-review-open-asks-for-it

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutCompletion/CheckoutStepPage.cs:92 | minor | flag argument `fromTheReview` picks between placing and redirecting; `placing` is ignored on the false path, so the call at `ConfirmationPageController.cs:42` reads as always placing | split into `PlacingTheOrderAsync(placing, ct)` and a public `ToTheReviewFirstAsync(ct)`; let the controller choose | open |
