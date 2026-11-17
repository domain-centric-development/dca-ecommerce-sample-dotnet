---
story: the-inventory-findings-are-closed
epic: guest-checkout
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (4 rows, 1 without a fitting value)
---

# Findings — the-inventory-findings-are-closed

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | ddd | product/spec/inventory/glossary.md:33 | minor | introduced |  | The two shops' inventory glossaries now describe a sale differently until the Java shop gets the same change. | `product/spec` is held equal in both shops (WP-107). This glossary now names `Sell`/`ReleaseUpTo` and says a sale raises no `StockReleased`, but the Java twin still releases and then decreases (plan `## Open assumptions`). The divergence comes from this story's own change. | Carry `Sell`/`ReleaseUpTo` into the Java sample's `StockLevel`, its two use cases and its glossary. | open | code | domain-concept | missing |
| 2 | clean-code | src/DcaShop.Inventory/Domain/Model/StockLevel.cs:72 | minor | introduced |  | The sale works out the new reservation before the decrease and overwrites it afterwards, and nothing says why that order matters. | `Sell` computes `reservedAfterSale` before `DecreaseStock` and then overwrites the reservation that `DecreaseStock` capped. That is correct only because reserved ≤ available. The order carries meaning that nothing names. | Extract the shared take-out-and-refuse step into a private method that `DecreaseStock` and `Sell` both call, or add one line at `:72` saying why the figure comes first. | open | code | comment | missing |
| 3 | clean-code | src/DcaShop.Inventory/Domain/Event/StockEvents.cs:20 | minor | introduced |  | The stock-decrease event's description talks about a reservation that the event does not carry. | The `StockDecreased` summary now says "a sale also consumes what was reserved for it". The record has no reservation figure, so the sentence describes the `Sell` operation, not this event's payload. | Keep the summary to the fact recorded ("Stock was taken out — a shipment, a sale or a correction downwards") and leave the reservation detail to `StockLevel.Sell`'s summary. | open | code | comment | incorrect |
| 4 | plan | src/DcaShop.Inventory/Domain/Model/StockLevel.cs:8-11 | minor | introduced |  | The planned mention of a sale in the stock record's description was left out on purpose. | The plan's `## Files` said the class summary should name the sale. The build left it as it was (`## Deviations from the plan`) because "every movement is recorded as a domain event" still holds. The reason holds and no criterion needs the line. | None needed. Recorded so the deviation is on file. | open | — | — | — |
