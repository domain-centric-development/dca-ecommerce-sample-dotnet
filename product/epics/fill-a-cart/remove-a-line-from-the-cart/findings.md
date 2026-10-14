---
story: remove-a-line-from-the-cart
epic: fill-a-cart
---

# Findings — remove-a-line-from-the-cart

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Cart/Application/Shopping/RemoveCartItem/RemoveCartItemUseCase.cs:34 | minor | no active cart is reported as the domain failure `CartItemNotFoundException`, a statement only a loaded cart can make (as planned) | throw a use-case failure for the missing active cart and catch it in `CartPageController.RemoveItem` | open |
| 2 | clean-code | src/DcaShop.Cart/Application/Shopping/RemoveCartItem/IRemoveCartItemInputPort.cs:5 | minor | `RemoveCartItem` beside `RemoveItemFromCart`: the names do not say one keys on the item id, the other on the product | name the key (e.g. `RemoveItemFromActiveCart`) or say it in a summary on both ports | open |
| 3 | hexagonal | src/DcaShop.Cart/Adapter/Incoming/Web/Shopping/CartPageController.cs:96 | minor | `CartNotModifiableException` in the filter cannot arise (only the active cart is loaded); the comment at :98 covers only the other case | drop it from the filter, or word the comment for both | open |
| 4 | hexagonal | tests/DcaShop.IntegrationTests/CartItemRemovalTest.cs:11 | minor | use case and adapter translation tested only together through HTTP; the no-active-cart path has no test | add a port test from `_factory.Services` and an adapter test with a stubbed port | open |
| 5 | clean-code | tests/DcaShop.E2eTests/RemoveFromCartE2eTest.cs:26 | minor | third private copy of `AddOneUnitAsync` (also `CartPageE2eTest.cs:29`, `HeaderBasketE2eTest.cs:29`) | extract into `BaseE2eTest` | open |
| 6 | clean-code | tests/DcaShop.E2eTests/Pages/CartPage.cs:119 | minor | own reload sentinel `window.__beforeRemove` beside the document-mark idiom of `BuyerInfoPage`/`PaymentPage` | one `ClickAndAwaitNewDocumentAsync` in `BasePage` | open |
