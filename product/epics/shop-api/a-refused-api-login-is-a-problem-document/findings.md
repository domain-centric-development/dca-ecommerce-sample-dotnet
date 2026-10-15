---
story: a-refused-api-login-is-a-problem-document
epic: shop-api
---

# Findings — a-refused-api-login-is-a-problem-document

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.E2eTests/LoginApiE2eTest.cs:18 | minor | The end-to-end tests now register a fresh API account the same way in three places. | The register-over-the-API block repeats `CartApiE2eTest.cs:48` and `Staff.cs:29` (same body, same `Created` assertion); Rule of Three reached. | Extract one E2E helper (e.g. `ApiAccounts.RegisterAsync(client, email)`) and call it from all three. | open |
| 2 | hexagonal | src/DcaShop.Account/Adapter/Incoming/Api/AuthResource.cs:57 | minor | The API's "log in with a token" challenge is spelled out in two places. | `WWW-Authenticate: Bearer` is written here and in `ShopIdentityAuthenticationHandler.cs:98`; a change to the scheme's challenge would have to be made twice. | Hold the challenge value in one constant beside the `ShopBearer` scheme and use it in both. | open |
