---
story: an-open-checkout-keeps-its-price
epic: prices
---

# Findings — an-open-checkout-keeps-its-price

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Checkout/Domain/Model/OrderFingerprint.cs:46 | minor | A changed quantity or product on a line is reported as a price change. | The line digest covers product, quantity and price, so any line difference becomes `PriceChangedSinceReviewException` (`CheckoutSession.cs:209`). The story's answer says a change other than a price gets "Your order has changed…". | Digest each line's identity apart from its price, and name a price change only when the identity agrees. | open |
| 2 | clean-code | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutCompletion/CheckoutStepPage.cs:198 | minor | The review's refusal comment still says changed prices are counted. | The comment, rewritten by this change, keeps "lines whose prices changed are counted". Nothing produces `ErrorType.PriceChanged` any more, so that branch is unreachable. `CheckoutValidationResult.cs:7,18` says the same. | Make the comments describe availability and stock only. | open |
| 3 | clean-code | src/DcaShop.Checkout/Application/CheckoutCompletion/GetReview/GetReviewUseCase.cs:28 | minor | The review and "Place Order" each turn article data into facts their own way. | The same mapping is in `ConfirmCheckoutUseCase.cs:57`. Both fingerprints depend on it, so a drift in either copy refuses every order. | Move the mapping into one `Application/Shared` helper and call it from both use cases. | open |
| 4 | clean-code | src/DcaShop.Checkout/Domain/Service/CheckoutPricing.cs:12 | minor | The checkout "pricing" no longer judges any price. | With `CalculateOrderTotal` and the price check gone, it only judges availability and stock. | Rename it after what it judges, together with the glossary and the Java twin, in a follow-up. | open |
