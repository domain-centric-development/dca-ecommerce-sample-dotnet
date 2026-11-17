---
story: stock-checks-answer-on-the-page
epic: guest-checkout
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (4 rows, 0 without a fitting value)
---

# Findings — stock-checks-answer-on-the-page

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:123 | minor | introduced |  |  | `Confirm` repeats `Submit`'s skeleton (`:185`): session lookup, `NoActiveSession()`, the step-guard catch and the refusal catch. Only the refusal answer differs. The guard decision itself is shared (`ToTheStepStillOpenAsync`, `:214`) | give `Submit` the refusal answer as a parameter and write `Confirm` through it, keeping its narrower catch | done | code | duplication | extraneous |
| 2 | clean-code | tests/DcaShop.E2eTests/Pages/ReviewPage.cs:30 | minor | introduced |  |  | `PressPlaceOrderAsync` is a second way to press the button beside `PlaceOrderAndShowNextPathAsync` | share the click body and name each method by what it waits for | done | test | duplication | extraneous |
| 3 | clean-code | tests/DcaShop.IntegrationTests/OutdatedCartStartTest.cs:30 | minor | introduced |  |  | one test holds both the port test and the adapter test, and its name states only the first | split it into a port test and an adapter test against `RefusingStart` | done | test | checking | incorrect |
| 4 | clean-code | tests/DcaShop.IntegrationTests/PlaceOrderRefusalPageTest.cs:44 | minor | introduced |  |  | `AnotherVisitorHasBoughtAsync` repeats the stock reduction that `OutdatedCartStartTest.cs:41` writes inline and `ShopOfItsOwn.AnotherGuestHasBoughtAsync` holds | move it into `GuestOrders` under one name for the other buyer and call it from both tests | done | test | duplication | extraneous |
