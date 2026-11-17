---
story: a-site-of-the-shops-own-can-embed-it
epic: fits-its-surroundings
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — a-site-of-the-shops-own-can-embed-it

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | plan | tests/DcaShop.E2eTests/EmbeddingPolicyE2eTest.cs:39 | minor | introduced |  |  | DisplayNames at :39, :55, :67 have no scenario in the shared specification; `SharedScenariosTest` fails once run with `-p:SpecificationPath` | Add the three scenarios to the specification's `scenarios.md` and the Java suite under the same titles | done | story | traceability | missing |
| 2 | test | tests/DcaShop.E2eTests/EmbeddedShopE2eTest.cs:74 | minor | introduced |  |  | The happy path asserts at least one cart item, not that the cart shows the product that was opened | Read the product title before adding and expect `[data-test='cart-item-name']` to have exactly that text, as `EmbeddingPolicyE2eTest.cs:64` does | done | test | checking | incorrect |
