---
story: add-a-product-to-the-cart
epic: fill-a-cart
---

# Findings — add-a-product-to-the-cart

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.E2eTests/AddToCartE2eTest.cs:12 | minor | DisplayName "Add to cart lands on the cart" has no scenario in the shared specification; `SharedScenariosTest` fails once run with `-p:SpecificationPath` | Add the scenario to the specification's `scenarios.md` and the Java suite under the same title | done |
| 2 | test | tests/DcaShop.IntegrationTests/CartPageTest.cs:23 | minor | The add POST's response is not checked, so a refused add would leave the Given unarranged and the test still green | Assert the POST answers a redirect to `/cart` | done |
