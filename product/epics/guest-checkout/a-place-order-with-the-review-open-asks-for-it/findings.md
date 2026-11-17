---
story: a-place-order-with-the-review-open-asks-for-it
epic: guest-checkout
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — a-place-order-with-the-review-open-asks-for-it

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutCompletion/CheckoutStepPage.cs:92 | minor | introduced |  |  | flag argument `fromTheReview` picks between placing and redirecting; `placing` is ignored on the false path, so the call at `ConfirmationPageController.cs:42` reads as always placing | split into `PlacingTheOrderAsync(placing, ct)` and a public `ToTheReviewFirstAsync(ct)`; let the controller choose | done | code | interface | incorrect |
