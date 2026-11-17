---
story: image-url-is-held-to-2000-characters
epic: shop-api
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — image-url-is-held-to-2000-characters

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | dca | tests/DcaShop.UnitTests/Product/ImageUrlTest.cs:8 | minor | introduced |  |  | Only the refusing side (2001) is tested; `>` turned into `>=` stays green | Add a fact that a value of exactly 2000 characters is accepted | done | test | checking | missing |
| 2 | clean-code | src/DcaShop.Product/Domain/Model/ImageUrl.cs:11 | minor | introduced |  |  | The limit 2000 is a bare literal in guard and message, as in `ProductDescription.cs:11` | Name it (`MaxLength`) on both value objects in one change | done | code | naming | missing |
