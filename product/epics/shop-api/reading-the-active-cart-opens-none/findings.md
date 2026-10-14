---
story: reading-the-active-cart-opens-none
epic: shop-api
---

# Findings — reading-the-active-cart-opens-none

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.E2eTests/CartApiE2eTest.cs:61 | minor | `SubjectOf` is a verbatim copy of the private `Staff.SubjectOf` (`tests/DcaShop.E2eTests/Staff.cs:57`) in the same test project | make `Staff.SubjectOf` internal (or one shared JWT helper) and delete the copy | open |
