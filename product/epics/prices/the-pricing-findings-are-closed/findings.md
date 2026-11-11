---
story: the-pricing-findings-are-closed
epic: prices
---

# Findings — the-pricing-findings-are-closed

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | ddd | product/spec/pricing/glossary.md:29 | minor | The price glossary now lists "change price" twice in a row, once for the price model and once for the operator's use case. | domain-term: after the rename, lines 29 and 30 of `ProductPrice`'s **Operations** both name `ChangePrice` — one the aggregate operation, one the use case — so one concept has two entries. | Merge line 30 into line 29 ("…; reached by operators through the staff API `PUT /api/products/{productId}/price` via the `ChangePrice` use case"). | open |
| 2 | clean-code | src/DcaShop.Pricing/Application/ProvisionInitialPrice/ProvisionInitialPriceUseCase.cs:11 | minor | The description of the initial-price step still says the price is "updated", the word the rename retired. | The class summary says "updates it afterwards" while the code now calls `ChangePrice` (:53). | Reword to "…and changes its price afterwards". | open |
| 3 | plan | product/spec/pricing/glossary.md:29 | minor | The two shops' pricing glossaries now name the price change differently until the Java shop gets the same rename. | deferred by the plan: the Java sample keeps `updatePrice` (story `## Notes`, plan `## Open assumptions`), so the shared `product/spec/pricing/glossary.md` differs between the shops. | Deliver the Java backlog item that renames `updatePrice` → `changePrice` with the same glossary lines. | open |
