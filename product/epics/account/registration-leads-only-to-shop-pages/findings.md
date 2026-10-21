---
story: registration-leads-only-to-shop-pages
epic: account
---

# Findings — registration-leads-only-to-shop-pages

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Account/Adapter/Incoming/Web/RegisterPageController.cs:118 | minor | The rule for which return pages count as the shop's own is written down twice, once for the login and once for the registration. | `ShopPage` is a copy of `LoginPageController.cs:108`; a tightening of the open-redirect rule has to be made in both controllers. | When a third Account page needs it, extract one internal helper in `Adapter/Incoming/Web` and call it from each controller. | open |
