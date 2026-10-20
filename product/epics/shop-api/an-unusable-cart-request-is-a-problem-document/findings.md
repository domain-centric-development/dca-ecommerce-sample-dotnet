---
story: an-unusable-cart-request-is-a-problem-document
epic: shop-api
---

# Findings — an-unusable-cart-request-is-a-problem-document

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.E2eTests/CartApiE2eTest.cs:68 | minor | The cart API tests open a cart for a new customer with the same four lines, now three times. | Register, open, assert 201, read the cart id repeated at lines 28–31, 48–51, 68–71. | Extract `OpenCartForNewCustomerAsync()` and use it in all three tests. | open |
| 2 | hexagonal | src/DcaShop.Cart/Adapter/Incoming/Api/UnacceptableValueProblemAttribute.cs:33 | minor | The cart API writes the title of its refusal in three places, which can drift apart. | "Unacceptable value" is a literal here, in `ShoppingCartResource.cs:145` and `CartApiExceptionHandler.cs:94`. | Hold it in one `internal const` of Cart's API adapter, as Pricing does (`ProductPriceResource.UnacceptableValue`). | open |
