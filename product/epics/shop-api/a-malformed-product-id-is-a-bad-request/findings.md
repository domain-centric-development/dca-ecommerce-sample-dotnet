---
story: a-malformed-product-id-is-a-bad-request
epic: shop-api
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (4 rows, 0 without a fitting value)
---

# Findings — a-malformed-product-id-is-a-bad-request

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | dca | src/DcaShop.Cart/Adapter/Incoming/Api/ShoppingCartResource.cs:158 | minor | introduced |  |  | 400 "Unacceptable value" now worded in the resource and in `CartApiExceptionHandler.cs:93`; the Java twin lets the parse exception reach its handler — same answer, two shapes | decide the shape once and record it as a catalog node under DCA-USE-020; align the other sample | open | code | duplication | extraneous |
| 2 | clean-code | tests/DcaShop.E2eTests/CartApiE2eTest.cs:79 | minor | introduced |  |  | a customer's client built with `Staff.Client`, and `CustomerTokenAsync` (:96) repeats `Staff.TokenAsync`'s registration | move registration and Bearer client to a neutral helper both use | open | test | duplication | extraneous |
| 3 | clean-code | src/DcaShop.Cart/Adapter/Incoming/Api/ShoppingCartResource.cs:143 | minor | introduced |  |  | parsed id named `product` here, `productId` in `AddItemToCart` | rename the local to `parsedProductId` | open | code | naming | incorrect |
| 4 | ddd | tests/DcaShop.IntegrationTests/CartItemRemovalResourceTest.cs:167 | minor | introduced |  |  | test name says "Line"; the Cart glossary prefers `CartItem` | rename to `…AndLeavesTheCartItem` | open | test | domain-term | incorrect |
