---
story: only-your-own-cart-through-the-api
epic: shop-api
---

# Findings — only-your-own-cart-through-the-api

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.IntegrationTests/ApiFlowTest.cs:68 | minor | reading-another-customers-cart arranges an empty cart, not the Given's "Domain-Driven Design" × 2; the 404 does not depend on contents, so the outcome is still proved | when the test is next touched, fill Ada's cart via `OpenCartHoldingTwoDomainDrivenDesignAsync` before the stranger's read | done |
