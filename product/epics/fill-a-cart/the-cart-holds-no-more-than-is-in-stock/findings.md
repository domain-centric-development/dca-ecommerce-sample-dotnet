---
story: the-cart-holds-no-more-than-is-in-stock
epic: fill-a-cart
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — the-cart-holds-no-more-than-is-in-stock

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Cart/Application/Shopping/AddItemToCart/AddItemToCartUseCase.cs:147 | minor | introduced |  | The rule that the same product adds up is worked out again outside the cart. | The use case sums `cart.QuantityOf(productId) + quantity` itself, repeating the merge rule `AddItem` holds; the plan's citation of `decision/where-authorization-and-validation-live.md` does not carry the placement (discriminator 3 puts a quantity limit over the aggregate's state inside it). It follows the plan, so it is not a deviation. | Let the aggregate answer the sum (`cart.QuantityAfterAdding(productId, quantity)`) or take the stock figure into `AddItem`, so a second path that grows a line states it once. | open | code | domain-invariant | incorrect |
| 2 | clean-code | src/DcaShop.Cart/Adapter/Incoming/Web/CartPageNotice.cs:40 | minor | introduced |  | The three figures of the stock refusal travel only by their order, so mixing two up would go unnoticed. | `Leave(..., params int[])` and `Sentence`'s `[requested, held, inStock]` pattern agree on position alone; the only test that reads the sentence has held = in stock = 8, so a swap stays green. | Add a named writer beside `Leave` (`LeaveNotEnoughStock(page, requested, held, inStock)`) so the order is written and read in one file. | open | code | interface | incorrect |
