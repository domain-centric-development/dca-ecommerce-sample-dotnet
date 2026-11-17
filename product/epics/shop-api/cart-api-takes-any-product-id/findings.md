---
story: cart-api-takes-any-product-id
epic: shop-api
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (3 rows, 0 without a fitting value)
---

# Findings — cart-api-takes-any-product-id

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Cart/Application/Shopping/AddItemToCart/AddItemToCartCommand.cs:5 | minor | introduced |  |  | one value, two names: the command's `string ProductId` is the failure's `ProductReference` | name both alike (`ProductReference` on the command; the wire keeps `productId`) | done | code | naming | incorrect |
| 2 | dca | src/DcaShop.Cart/Application/Shopping/AddItemToCart/AddItemToCartUseCase.cs:57 | minor | introduced |  |  | the use case parses a Guid itself, though `ProductId` owns its parsing (`src/DcaShop.SharedKernel/Domain/Model/ProductId.cs:10`) | add `ProductId.TryOf(string, out ProductId)` and call it | done | code | domain-concept | missing |
| 3 | dca | .dca-factory/runs/cart-api-takes-any-product-id/plan.md:1 | minor | introduced |  |  | the harness questions (catalog / rule / marker) are not answered; the MCP adapter parses at the edge (`src/DcaShop.Product/Adapter/Incoming/Mcp/ProductCatalogMcpToolProvider.cs:59`), this use case inside | record the answer in WP-107: catalog decision node on where an unparseable id becomes not-found; rule none; marker none | done | plan | documentation | missing |
