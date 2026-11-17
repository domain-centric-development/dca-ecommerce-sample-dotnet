---
story: the-cart-oversight-totals-at-todays-prices
epic: shop-api
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — the-cart-oversight-totals-at-todays-prices

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Cart/Application/CartOversight/GetAllCarts/GetAllCartsUseCase.cs:11 | minor | introduced |  | The comment claims every answer about one cart uses today's prices, but several still do not. | The remark says "as every answer about one cart is"; add, remove, recover, merge and merge-options results still total with `ShoppingCart.CalculateTotal()` (prices at addition), e.g. `AddItemToCartUseCase.cs:50`. | Say "as the single-cart answer (`GetCartById`) is" until `every-cart-answer-shows-todays-price` lands. | open | code | comment | incorrect |
