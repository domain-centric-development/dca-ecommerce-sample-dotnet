---
story: taking-over-the-guest-cart-is-a-step-of-its-own
epic: shop-with-an-account
---

# Findings — taking-over-the-guest-cart-is-a-step-of-its-own

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Cart/Adapter/Incoming/Web/CartRecovery/CartMergePageController.cs:111 | minor | the take-over POST runs `RecoverCart` unconditionally; with a page now between query and command, an account cart filled meanwhile is merged without the choice (`RecoverCartUseCase.cs:55` adds into any account cart) | let `RecoverCart` re-check inside its transaction that the account cart is empty and answer an outcome that sends the visitor back to `/cart/merge` | open |
| 2 | hexagonal | src/DcaShop.Cart/Application/CartRecovery/GetCartMergeOptions/GetCartMergeOptionsResult.cs:13 | minor | three outcomes encoded as two flags (`MergeRequired`, defaulted `TakeOverRequired`); the impossible pair is representable and the controller depends on the check order | one outcome value set by the three factories, the controller switches on it | open |
| 3 | hexagonal | src/DcaShop.Cart/Adapter/Incoming/Web/CartRecovery/CartMergePageController.cs:111 | minor | the new endpoint takes `anonymousUserId` from the form and redirects to an unchecked `returnUrl`, as `Decide` already does | derive the guest identity from a protected value and redirect via `LocalRedirect`/`Url.IsLocalUrl` (backlog, with `Show`/`Decide`) | open |
| 4 | hexagonal | tests/DcaShop.IntegrationTests/CartTakeOverPageTest.cs:26 | minor | the POST `take-over` translation (command with the current identity, redirect, `returnUrl`) is covered only through the wired shop, not against a stubbed port | add an adapter fact posting the form against `RecordingRecovery` | open |
| 5 | clean-code | tests/DcaShop.IntegrationTests/CartRecoveryOnLoginTest.cs:318 | minor | the open-page/send-form/assert-redirect round trip is copied in three files (also `AccountFlowTest.cs:65`, `CartStaysWithThePersonTest.cs:157`) | one `TakeOverForm.FollowAsync(browser, location)` used by all three | open |
| 6 | clean-code | tests/DcaShop.IntegrationTests/CartTakeOverPageTest.cs:67 | minor | `TakeOverForm`, shared by four test classes, sits at the bottom of one test's file | move it into `TakeOverForm.cs` | open |
| 7 | clean-code | tests/DcaShop.E2eTests/Pages/CartTakeOverPage.cs:28 | minor | the wait for a URL off `/cart/merge` is covered by `CartPage.OpenAsync`'s wait for exactly `/cart` | delete the line | open |
