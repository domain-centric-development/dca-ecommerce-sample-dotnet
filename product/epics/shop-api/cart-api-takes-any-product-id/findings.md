---
story: cart-api-takes-any-product-id
epic: shop-api
---

# Findings — cart-api-takes-any-product-id

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Cart/Application/Shopping/AddItemToCart/AddItemToCartCommand.cs:5 | minor | one value, two names: the command's `string ProductId` is the failure's `ProductReference` | name both alike (`ProductReference` on the command; the wire keeps `productId`) | open |
| 2 | dca | src/DcaShop.Cart/Application/Shopping/AddItemToCart/AddItemToCartUseCase.cs:57 | minor | the use case parses a Guid itself, though `ProductId` owns its parsing (`src/DcaShop.SharedKernel/Domain/Model/ProductId.cs:10`) | add `ProductId.TryOf(string, out ProductId)` and call it | open |
| 3 | dca | .dca-factory/runs/cart-api-takes-any-product-id/plan.md:1 | minor | the harness questions (catalog / rule / marker) are not answered; the MCP adapter parses at the edge (`src/DcaShop.Product/Adapter/Incoming/Mcp/ProductCatalogMcpToolProvider.cs:59`), this use case inside | record the answer in WP-107: catalog decision node on where an unparseable id becomes not-found; rule none; marker none | open |
