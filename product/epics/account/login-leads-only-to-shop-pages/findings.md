---
story: login-leads-only-to-shop-pages
epic: account
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — login-leads-only-to-shop-pages

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.E2eTests/Pages/LoginPage.cs:73 | minor | introduced |  | The test helper that reads where a link leads uses a different word than its siblings. | `RegisterLinkTargetAsync` reads an `href`, which the other page objects call `…PathAsync` (`HomePage.cs:142` `CardLinkPathAsync`). | Rename to `RegisterLinkPathAsync`. | open | test | naming | incorrect |
