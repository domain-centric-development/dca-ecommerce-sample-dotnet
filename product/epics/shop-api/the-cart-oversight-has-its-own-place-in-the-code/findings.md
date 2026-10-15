---
story: the-cart-oversight-has-its-own-place-in-the-code
epic: shop-api
---

# Findings — the-cart-oversight-has-its-own-place-in-the-code

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Cart/Application/CartOversight/GetAllCarts/GetAllCartsUseCase.cs:7 | minor | The moved cart list still calls the staff "operators" in its comments. | Doc comments here and in `GetAllCartsResult.cs:6` say "operator role" / "as an operator sees them" while the feature's glossary entry and the gate say staff. | Say "staff" in both comments. | open |
| 2 | clean-code | src/DcaShop.Cart/Adapter/Incoming/Api/ShoppingCartResource.cs:25 | minor | The cart resource's note about token login now reads as if it belonged to the list that moved away. | The remark paragraph opens with the pointer to `CartOversightResource`, and the Bearer sentence that holds for every route of this class hangs under it. | Give the Bearer sentence its own paragraph; keep the pointer as one sentence. | open |
