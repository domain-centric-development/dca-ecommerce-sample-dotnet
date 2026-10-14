---
story: accounts-start-fresh-with-the-shop
epic: account
---

# Findings — accounts-start-fresh-with-the-shop

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | plan | tests/DcaShop.E2eTests/ShopRestartE2eTest.cs:14 | minor | DisplayName "Restart turns the customer into a visitor" has no scenario in the shared specification's `scenarios.md`, so `tests/DcaShop.UnitTests/Specification/SharedScenariosTest.cs` fails when it runs with `-p:SpecificationPath` (plan, open assumption 5) | Add the scenario under this title to the specification and port the test to the Java E2E suite under the same title | done |
