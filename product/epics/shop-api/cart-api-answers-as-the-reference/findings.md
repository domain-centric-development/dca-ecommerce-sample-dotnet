---
story: cart-api-answers-as-the-reference
epic: shop-api
---

# Findings — cart-api-answers-as-the-reference

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Cart/Adapter/Incoming/Api/ShoppingCartDtoConverter.cs:35 | minor | `ToListDto` still sends `Active` while `ToDto` (:21) sends `ACTIVE`, so one resource spells `CartStatus` two ways and the wire value depends on the enum member's name. The plan scopes the list out on purpose. | Map `CartStatus` to its published value explicitly in one converter method and use it in both `ToDto` and `ToListDto` (a follow-up story, as the plan says). | open |
| 2 | clean-code | src/DcaShop.Cart/Domain/Model/ShoppingCart.cs:95 | minor | `RemoveItem(CartItemId)` has had no production caller since the use case switched to `RemoveItemByProductId`. Only `ShoppingCartTest.cs:53` keeps it alive, and glossary line 41 still lists it. | Decide whether removing a cart item by its own id is still a domain operation; if not, remove it together with its test and glossary entry. | open |
| 3 | ddd | src/DcaShop.Cart/Application/Shopping/RemoveItemFromCart/RemoveItemFromCartCommand.cs:3 | minor | The doc comments here and at `ShoppingCartResource.cs:127` say "line", which leans on the glossary's avoided synonyms ("Line Item", "Position", glossary.md:59). | Write "the cart item holding …". | open |
