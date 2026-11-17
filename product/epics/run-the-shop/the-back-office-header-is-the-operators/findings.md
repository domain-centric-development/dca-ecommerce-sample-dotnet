---
story: the-back-office-header-is-the-operators
epic: run-the-shop
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — the-back-office-header-is-the-operators

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.IntegrationTests/BackofficeHeaderTest.cs:334 | minor | introduced |  | The new header test has its own copy of the form-posting helper the other tests already share. | `Form(markup, fields)` repeats `Forms.Submission` (`tests/DcaShop.IntegrationTests/Forms.cs:14`) line for line: the token regex, the non-empty check and appending the token | Delete `Form` and call `Forms.Submission(page, …)` in `LogInAsync` and `RegisterAsync` | open | test | duplication | extraneous |
