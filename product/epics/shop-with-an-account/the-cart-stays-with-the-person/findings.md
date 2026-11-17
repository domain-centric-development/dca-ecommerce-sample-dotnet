---
story: the-cart-stays-with-the-person
epic: shop-with-an-account
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — the-cart-stays-with-the-person

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.E2eTests/RegistrationKeepsTheCartE2eTest.cs:15 | minor | introduced |  |  | DisplayName "Registering keeps the guest cart" has no scenario in the shared specification, so `SharedScenariosTest` fails with `-p:SpecificationPath` | add the scenario to the specification's `scenarios.md` and the Java suite under the same title | done | story | traceability | missing |
