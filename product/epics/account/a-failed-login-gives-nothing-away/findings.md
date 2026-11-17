---
story: a-failed-login-gives-nothing-away
epic: account
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — a-failed-login-gives-nothing-away

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.IntegrationTests/AccountFlowTest.cs:98 | minor | introduced |  |  | The unknown-address test looks for the message anywhere in the body, not in `login-error-message` the way `LoginPageTest.cs:88` reads it | Read the text of `data-test="login-error-message"` and compare it with Equal | done | test | checking | incorrect |
