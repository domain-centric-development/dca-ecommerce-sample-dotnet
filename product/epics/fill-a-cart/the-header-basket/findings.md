---
story: the-header-basket
epic: fill-a-cart
---

# Findings — the-header-basket

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.E2eTests/HeaderBasketE2eTest.cs:18 | minor | The DisplayName "Basket dropdown lists the cart" has no scenario in the shared specification, so `SharedScenariosTest` fails once run with `-p:SpecificationPath` | Add the scenario to the specification's `scenarios.md` and the Java suite under the same title | done |
