---
story: registration-says-the-account-was-created
epic: account
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (5 rows, 0 without a fitting value)
---

# Findings — registration-says-the-account-was-created

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Account/Adapter/Incoming/Web/RegisterPageController.cs:86 | minor | introduced |  |  | key written from raw `returnUrl`, read from `Request.Path`+`QueryString` (`AccountNoticeViewComponent.cs:20`); a target with an encoded path or a fragment is never shown its notice | build both keys through one normalising helper in `AccountPageNotices` | open | code | algorithm | incorrect |
| 2 | clean-code | src/DcaShop.Account/Adapter/Incoming/Web/AccountPageNotice.cs:58 | minor | introduced |  |  | parameter `page` of `Leave`/`TakeAccountPageNotice` holds a TempData key, not a page address | rename it `key` | open | code | naming | incorrect |
| 3 | clean-code | tests/DcaShop.E2eTests/Pages/BasePage.cs:123 | minor | introduced |  |  | `NoticesAsync` reads only `account-notice`, not every notice | rename `AccountNoticesAsync` | open | test | naming | incorrect |
| 4 | clean-code | src/DcaShop.Account/Adapter/Incoming/Web/AccountPageNotice.cs:6 | minor | introduced |  |  | summary line runs past the file's wrap width | re-wrap the paragraph | open | code | comment | incorrect |
| 5 | dca | product/tech.md:33 | minor | introduced |  |  | Runtime still calls `AccountPageNotice` the account pages' notices; it now addresses any page via the layout's `AccountNotice` fragment | say so in tech.md Runtime; record the twin follow-up (`account-notice`, `account-created`) | open | docs | documentation | incorrect |
