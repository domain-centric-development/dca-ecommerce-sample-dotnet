---
story: an-unusable-product-request-is-a-problem-document
epic: shop-api
---

# Findings — an-unusable-product-request-is-a-problem-document

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | hexagonal | src/DcaShop.Product/Adapter/Incoming/Api/CreateProductRequest.cs:10 | minor | A missing SKU or category is refused with the framework's sentence instead of the shop's. | `Sku` (:10) and `Category` (:15) keep a bare `[Required]`, so their reason is "The Sku field is required." under the new title. | Give both an `ErrorMessage` in the shop's words. | open |
| 2 | ddd | src/DcaShop.Product/Adapter/Incoming/Api/CreateProductRequest.cs:11 | minor | A name that is too long is still refused without saying which field was wrong. | The 255-character half of the name rule stays `ProductName`'s `ArgumentException` (`ProductApiExceptionHandler.cs:72`): same title, no `errors`. | Add `[StringLength(255, ErrorMessage = …)]` in a follow-up story. | open |
| 3 | hexagonal | src/DcaShop.Product/Adapter/Incoming/Api/UnacceptableValueProblemAttribute.cs:35 | minor | An unreadable value passes the serializer's technical message to the partner. | The filter copies every `ModelState` entry, including input-formatter errors keyed `$.price` with exception text. | Word unreadable-body entries in the filter, or turn off `AllowInputFormatterExceptionMessages`. | open |
| 4 | clean-code | tests/DcaShop.IntegrationTests/ProductPriceAndStockRulesTest.cs:25 | minor | The older price and stock refusal tests now repeat weaker copies of the new ones. | `APriceOfZeroIsRefused…` (:25) and `ANegativeInitialStockIsRefused…` (:99) send the same bodies and assert a subset. | Remove them once the scenarios' `Test:` lines point to the new tests. | open |
