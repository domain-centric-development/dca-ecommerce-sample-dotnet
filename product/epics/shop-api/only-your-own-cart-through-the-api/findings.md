---
story: only-your-own-cart-through-the-api
epic: shop-api
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — only-your-own-cart-through-the-api

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.IntegrationTests/ApiFlowTest.cs:68 | minor | introduced |  |  | reading-another-customers-cart arranges an empty cart, not the Given's "Domain-Driven Design" × 2; the 404 does not depend on contents, so the outcome is still proved | when the test is next touched, fill Ada's cart via `OpenCartHoldingTwoDomainDrivenDesignAsync` before the stranger's read | done | test | traceability | incorrect |
