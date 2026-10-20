---
story: two-orders-never-sell-more-than-the-stock
epic: guest-checkout
---

# Findings — two-orders-never-sell-more-than-the-stock

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Inventory/Application/ReduceStock/ReduceStockUseCase.cs:92 | minor | A sale is recorded as a cancelled reservation followed by a decrease. | Every sale with a reservation raises `StockReleased`, which the Inventory glossary defines as a cancelled reservation. | The document stage brings the glossary's `DecreaseStock`/`StockReleased` entries in line; a later story may add a `StockLevel` operation that sells against the reservation. | open |
| 2 | clean-code | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutStepNotice.cs:69 | minor | A buyer whose prices changed is told items are unavailable or out of stock. | `PricesChanged` is now reached only without a short line, yet its sentence still names stock; the plan kept the wording on purpose (open assumption 2). | A product decision: word it from changed prices, in `refusal-wording.md` and both shops. | open |
| 3 | clean-code | src/DcaShop.Inventory/Application/ReleaseStock/ReleaseStockUseCase.cs:43 | minor | Two use cases work out the stock arithmetic the stock record should own. | `Math.Min(quantity, ReservedQuantity)` and `DecreaseStock`'s precondition are repeated in `ReleaseStockUseCase` and `ReduceStockUseCase.cs:89`. | Move to `StockLevel` (release-up-to, sell-against-reservation), one call per use case. | open |
| 4 | clean-code | tests/DcaShop.E2eTests/BaseE2eTest.cs:89 | minor | The walk to the review is now kept in three copies in the browser tests. | Shared `GuestFromTheCartToTheReviewAsync` added; private copies remain in `StockGoesDownE2eTest.cs:37` and `PlaceOrderRefusedE2eTest.cs:43`. | Delete the two private copies and call the shared one. | open |
