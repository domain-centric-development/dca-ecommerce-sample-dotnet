---
story: a-malformed-product-id-is-a-bad-request
epic: shop-api
---

# Findings — a-malformed-product-id-is-a-bad-request

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | dca | src/DcaShop.Cart/Adapter/Incoming/Api/ShoppingCartResource.cs:158 | minor | 400 "Unacceptable value" now worded in the resource and in `CartApiExceptionHandler.cs:93`; the Java twin lets the parse exception reach its handler — same answer, two shapes | decide the shape once and record it as a catalog node under DCA-USE-020; align the other sample | open |
| 2 | clean-code | tests/DcaShop.E2eTests/CartApiE2eTest.cs:79 | minor | a customer's client built with `Staff.Client`, and `CustomerTokenAsync` (:96) repeats `Staff.TokenAsync`'s registration | move registration and Bearer client to a neutral helper both use | open |
| 3 | clean-code | src/DcaShop.Cart/Adapter/Incoming/Api/ShoppingCartResource.cs:143 | minor | parsed id named `product` here, `productId` in `AddItemToCart` | rename the local to `parsedProductId` | open |
| 4 | ddd | tests/DcaShop.IntegrationTests/CartItemRemovalResourceTest.cs:167 | minor | test name says "Line"; the Cart glossary prefers `CartItem` | rename to `…AndLeavesTheCartItem` | open |
