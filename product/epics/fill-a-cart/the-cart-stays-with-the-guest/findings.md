---
story: the-cart-stays-with-the-guest
epic: fill-a-cart
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — the-cart-stays-with-the-guest

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | plan | tests/DcaShop.E2eTests/GuestCartE2eTest.cs:22 | minor | introduced |  |  | DisplayName "Cart kept across visits" has no scenario in the shared specification; `SharedScenariosTest` fails once run with `-p:SpecificationPath` | Add the scenario to the specification's `scenarios.md` and the Java suite under the same title | done | story | traceability | missing |
