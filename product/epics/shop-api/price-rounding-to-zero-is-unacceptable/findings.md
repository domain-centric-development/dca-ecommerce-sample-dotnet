---
story: price-rounding-to-zero-is-unacceptable
epic: shop-api
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — price-rounding-to-zero-is-unacceptable

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | hexagonal | src/DcaShop.Product/Adapter/Incoming/Api/ProductApiExceptionHandler.cs:69 | minor | introduced |  |  | The arm's comment names request validation as the way out of the `ArgumentException` ambiguity; this story deliberately leaves a price that rounds to zero to `Price`, and nothing at the translation site says so | Add one sentence: a value refused only after the domain normalised it (a price that rounds to zero) stays the value object's refusal (decision `creating-a-product-holds-its-price-01`) | done | code | comment | missing |
| 2 | dca | tests/DcaShop.E2eTests/CreateProductApiE2eTest.cs:44 | minor | introduced |  |  | `DisplayName` "A price that rounds to zero is refused" has no title in the specification's `scenarios.md`; `SharedScenariosTest` fails on it once run with `SpecificationPath` (pre-existing for the neighbouring "Staff create a product"; the plan records it as an open assumption) | The user adds the scenario to the specification (and the Java suite takes the title), or moves the HTTP-only API tests out of the `*E2eTest.cs` set | done | story | traceability | missing |
