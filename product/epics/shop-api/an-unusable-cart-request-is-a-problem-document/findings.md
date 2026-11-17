---
story: an-unusable-cart-request-is-a-problem-document
epic: shop-api
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — an-unusable-cart-request-is-a-problem-document

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.E2eTests/CartApiE2eTest.cs:68 | minor | introduced |  | The cart API tests open a cart for a new customer with the same four lines, now three times. | Register, open, assert 201, read the cart id repeated at lines 28–31, 48–51, 68–71. | Extract `OpenCartForNewCustomerAsync()` and use it in all three tests. | open | test | duplication | extraneous |
| 2 | hexagonal | src/DcaShop.Cart/Adapter/Incoming/Api/UnacceptableValueProblemAttribute.cs:33 | minor | introduced |  | The cart API writes the title of its refusal in three places, which can drift apart. | "Unacceptable value" is a literal here, in `ShoppingCartResource.cs:145` and `CartApiExceptionHandler.cs:94`. | Hold it in one `internal const` of Cart's API adapter, as Pricing does (`ProductPriceResource.UnacceptableValue`). | open | code | duplication | extraneous |
