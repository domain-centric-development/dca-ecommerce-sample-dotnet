---
story: created-product-answers-with-its-price
epic: shop-api
---

# Findings — created-product-answers-with-its-price

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | dca | src/DcaShop.Web/Views/Product/Detail.cshtml:35 | minor | The fallback text is chosen in the Razor view. The Java twin chooses it in its view model (`ProductDetailPageViewModel.java:59`), so the view no longer mirrors the template one to one. The plan's open assumption about the Java side was wrong. | Choose the text in `ProductPageController.cs:44` when it builds `ProductDetailPageViewModel`, and keep the view's description block as it was. | open |
| 2 | clean-code | src/DcaShop.Product/Adapter/Incoming/Api/ProductDto.cs:4 | minor | The doc comment still says price is absent from the answer to a write. After this change the create answer carries `Price`/`Currency`. | Say that stock and availability stay absent on a create, and that price and currency echo the price the product was created with. | open |
| 3 | ddd | src/DcaShop.Product/Application/CreateProduct/CreateProductResult.cs:5 | minor | The member is named `Price`. The glossary (`product/spec/product/glossary.md:31`) calls this value the *initial price*, and Pricing owns the current one. | Rename it to `InitialPrice` and keep the wire field `price`. | open |
