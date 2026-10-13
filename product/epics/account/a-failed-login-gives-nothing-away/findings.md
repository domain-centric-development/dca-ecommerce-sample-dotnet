---
story: a-failed-login-gives-nothing-away
epic: account
---

# Findings — a-failed-login-gives-nothing-away

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.IntegrationTests/AccountFlowTest.cs:98 | minor | The unknown-address test looks for the message anywhere in the body, not in `login-error-message` the way `LoginPageTest.cs:88` reads it | Read the text of `data-test="login-error-message"` and compare it with Equal | open |
