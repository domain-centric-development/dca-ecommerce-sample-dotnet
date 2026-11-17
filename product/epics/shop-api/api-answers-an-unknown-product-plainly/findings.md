---
story: api-answers-an-unknown-product-plainly
epic: shop-api
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — api-answers-an-unknown-product-plainly

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Web/Program.cs:25 | minor | introduced |  |  | The comment still calls problem documents *the* answer format of /api/** and /mcp, but an unmatched address there now answers with its bare status | Say that a failure a context names answers with a problem document and an unmatched address with its bare status | done | code | comment | incorrect |
| 2 | clean-code | src/DcaShop.Web/Program.cs:87 | minor | introduced |  |  | The comment names only an unmatched route, but the switch drops the HTML page for every bodiless status on the token-only paths (e.g. a bare 403) | Name it as any bodiless error on /api/** and /mcp, an unmatched address being one | done | code | comment | incorrect |
