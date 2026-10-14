---
story: stock-checks-answer-on-the-page
epic: guest-checkout
---

# Findings — stock-checks-answer-on-the-page

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:123 | minor | `Confirm` repeats `Submit`'s skeleton (`:185`): session lookup, `NoActiveSession()`, the step-guard catch and the refusal catch. Only the refusal answer differs. The guard decision itself is shared (`ToTheStepStillOpenAsync`, `:214`) | give `Submit` the refusal answer as a parameter and write `Confirm` through it, keeping its narrower catch | open |
| 2 | clean-code | tests/DcaShop.E2eTests/Pages/ReviewPage.cs:30 | minor | `PressPlaceOrderAsync` is a second way to press the button beside `PlaceOrderAndShowNextPathAsync` | share the click body and name each method by what it waits for | open |
| 3 | clean-code | tests/DcaShop.IntegrationTests/OutdatedCartStartTest.cs:30 | minor | one test holds both the port test and the adapter test, and its name states only the first | split it into a port test and an adapter test against `RefusingStart` | open |
| 4 | clean-code | tests/DcaShop.IntegrationTests/PlaceOrderRefusalPageTest.cs:44 | minor | `AnotherVisitorHasBoughtAsync` repeats the stock reduction that `OutdatedCartStartTest.cs:41` writes inline and `ShopOfItsOwn.AnotherGuestHasBoughtAsync` holds | move it into `GuestOrders` under one name for the other buyer and call it from both tests | done |
