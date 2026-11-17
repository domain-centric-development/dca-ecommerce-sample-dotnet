---
story: add-a-product-to-the-cart
epic: fill-a-cart
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — add-a-product-to-the-cart

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.E2eTests/AddToCartE2eTest.cs:12 | minor | introduced |  |  | DisplayName "Add to cart lands on the cart" has no scenario in the shared specification; `SharedScenariosTest` fails once run with `-p:SpecificationPath` | Add the scenario to the specification's `scenarios.md` and the Java suite under the same title | done | story | traceability | missing |
| 2 | test | tests/DcaShop.IntegrationTests/CartPageTest.cs:23 | minor | introduced |  |  | The add POST's response is not checked, so a refused add would leave the Given unarranged and the test still green | Assert the POST answers a redirect to `/cart` | done | test | checking | missing |
