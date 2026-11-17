---
story: the-cart-oversight-has-its-own-place-in-the-code
epic: shop-api
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — the-cart-oversight-has-its-own-place-in-the-code

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Cart/Application/CartOversight/GetAllCarts/GetAllCartsUseCase.cs:7 | minor | introduced |  | The moved cart list still calls the staff "operators" in its comments. | Doc comments here and in `GetAllCartsResult.cs:6` say "operator role" / "as an operator sees them" while the feature's glossary entry and the gate say staff. | Say "staff" in both comments. | open | code | domain-term | incorrect |
| 2 | clean-code | src/DcaShop.Cart/Adapter/Incoming/Api/ShoppingCartResource.cs:25 | minor | introduced |  | The cart resource's note about token login now reads as if it belonged to the list that moved away. | The remark paragraph opens with the pointer to `CartOversightResource`, and the Bearer sentence that holds for every route of this class hangs under it. | Give the Bearer sentence its own paragraph; keep the pointer as one sentence. | open | code | comment | incorrect |
