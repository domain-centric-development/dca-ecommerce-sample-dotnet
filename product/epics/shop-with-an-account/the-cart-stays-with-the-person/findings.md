---
story: the-cart-stays-with-the-person
epic: shop-with-an-account
---

# Findings — the-cart-stays-with-the-person

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.E2eTests/RegistrationKeepsTheCartE2eTest.cs:15 | minor | DisplayName "Registering keeps the guest cart" has no scenario in the shared specification, so `SharedScenariosTest` fails with `-p:SpecificationPath` | add the scenario to the specification's `scenarios.md` and the Java suite under the same title | open |
