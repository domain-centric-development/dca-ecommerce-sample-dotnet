---
story: only-the-browsers-own-guest-cart-is-taken-over
epic: shop-with-an-account
---

# Findings — only-the-browsers-own-guest-cart-is-taken-over

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | hexagonal | src/DcaShop.Account/Adapter/Incoming/Web/LoginPageController.cs:78 | minor | Logging into a second account without logging out records the first account as the guest whose cart may follow. | `KeepGuestBeforeLogin(guest)` runs whatever the identity was; a registered one is minted as an anonymous token, so the port's "anonymous identity the login replaced" is not what the adapter delivers. The re-login merge itself predates the story. | Keep the guest only when the captured identity `IsAnonymous`; otherwise write no guest cookie. | open |
| 2 | clean-code | src/DcaShop.Cart/Application/CartRecovery/RecoverCart/RecoverCartUseCase.cs:16 | minor | The take-over's description says it does nothing for matching identities, but it now refuses first. | Remark "idempotent: with matching identities … it does nothing" is stale; the ownership guard at `:39` runs before the identity check at `:44`. | Name the refusal in the remark, or check matching identities before the guard. | open |
| 3 | clean-code | tests/DcaShop.E2eTests/AnotherGuestsCartE2eTest.cs:77 | minor | The browser tests now read a login token's subject in three separate copies. | `GuestOfAsync` repeats the JWT payload decoding of `Staff.cs:59` and `CartApiE2eTest.cs:146`. | Extract one shared test helper `SubjectOf(jwt)` and use it in all three. | open |
