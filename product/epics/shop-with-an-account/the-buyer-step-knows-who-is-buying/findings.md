---
story: the-buyer-step-knows-who-is-buying
epic: shop-with-an-account
---

# Findings — the-buyer-step-knows-who-is-buying

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.E2eTests/BuyerStepKnowsWhoIsBuyingE2eTest.cs:15 | minor | DisplayName "Logged in customer sees who is buying" has no scenario in the shared specification, so `SharedScenariosTest` fails when run with `SpecificationPath` | add the scenario to the specification's `scenarios.md` (and the Java suite), or carry the criterion under the existing `scenario.checkout.buyer-step-reflects-login` title | open |
