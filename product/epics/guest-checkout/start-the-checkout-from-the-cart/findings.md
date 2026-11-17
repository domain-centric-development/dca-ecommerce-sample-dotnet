---
story: start-the-checkout-from-the-cart
epic: guest-checkout
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — start-the-checkout-from-the-cart

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | adopt | tests/DcaShop.E2eTests/StartCheckoutE2eTest.cs:12 | minor | introduced |  |  | DisplayName "Proceed opens the buyer step" has no scenario in the shared specification; `SharedScenariosTest` fails once run with `-p:SpecificationPath` | Add the scenario to the specification's `scenarios.md` and to the Java suite under the same title | done | story | traceability | missing |
