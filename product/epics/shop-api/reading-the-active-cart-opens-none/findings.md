---
story: reading-the-active-cart-opens-none
epic: shop-api
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — reading-the-active-cart-opens-none

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.E2eTests/CartApiE2eTest.cs:61 | minor | introduced |  |  | `SubjectOf` is a verbatim copy of the private `Staff.SubjectOf` (`tests/DcaShop.E2eTests/Staff.cs:57`) in the same test project | make `Staff.SubjectOf` internal (or one shared JWT helper) and delete the copy | open | test | duplication | extraneous |
