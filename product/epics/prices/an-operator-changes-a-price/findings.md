---
story: an-operator-changes-a-price
epic: prices
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — an-operator-changes-a-price

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Pricing/Adapter/Incoming/Api/ProductPriceResource.cs:49 | minor | introduced |  | The "price above zero" rule is checked twice, once in the API and once in the model, and the model reports it as a plain programming error. | The resource checks the raw decimal (`request.Price <= 0`) while `Price`/`Money` refuse after rounding to cents with `ArgumentException` (`src/DcaShop.SharedKernel/Domain/Model/Price.cs:13`); `{"price":0.004}` passes the edge and comes back through `PricingApiExceptionHandler.cs:47` as a 400 without `errors.price`. Same pattern as the catalogue's handler — no regression; build.md records it as a follow-up for both samples. | Named domain failures for `Price`/`Money`, mapped in the handlers, and the adapter's copy of the rule dropped — in both samples together. | open | code | domain-invariant | incorrect |
| 2 | ddd | src/DcaShop.Pricing/Domain/Model/ProductPrice.cs:41 | minor | introduced |  | The operation is called "change" everywhere except in the price model itself, which still says "update". | The application side is `ChangePrice` (DCA-NAM-013); the aggregate method stays `ProductPrice.UpdatePrice`, and the glossary's operation line names it (`product/spec/pricing/glossary.md:29`). build.md records it as a follow-up for both samples. | Rename `ProductPrice.UpdatePrice` to `ChangePrice` with the glossary line, in both samples. | done | code | domain-term | incorrect |
