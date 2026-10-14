---
story: registration-says-the-account-was-created
epic: account
---

# Findings — registration-says-the-account-was-created

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Account/Adapter/Incoming/Web/RegisterPageController.cs:86 | minor | key written from raw `returnUrl`, read from `Request.Path`+`QueryString` (`AccountNoticeViewComponent.cs:20`); a target with an encoded path or a fragment is never shown its notice | build both keys through one normalising helper in `AccountPageNotices` | open |
| 2 | clean-code | src/DcaShop.Account/Adapter/Incoming/Web/AccountPageNotice.cs:58 | minor | parameter `page` of `Leave`/`TakeAccountPageNotice` holds a TempData key, not a page address | rename it `key` | open |
| 3 | clean-code | tests/DcaShop.E2eTests/Pages/BasePage.cs:123 | minor | `NoticesAsync` reads only `account-notice`, not every notice | rename `AccountNoticesAsync` | open |
| 4 | clean-code | src/DcaShop.Account/Adapter/Incoming/Web/AccountPageNotice.cs:6 | minor | summary line runs past the file's wrap width | re-wrap the paragraph | open |
| 5 | dca | product/tech.md:33 | minor | Runtime still calls `AccountPageNotice` the account pages' notices; it now addresses any page via the layout's `AccountNotice` fragment | say so in tech.md Runtime; record the twin follow-up (`account-notice`, `account-created`) | open |
