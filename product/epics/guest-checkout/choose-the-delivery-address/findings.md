---
story: choose-the-delivery-address
epic: guest-checkout
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — choose-the-delivery-address

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.E2eTests/ChooseDeliveryAddressE2eTest.cs:18 | minor | introduced |  |  | The display name "Address and shipping open the payment step" has no scenario in the shared specification, so `SharedScenariosTest` fails when run with `SpecificationPath` | Add the scenario to the specification's `scenarios.md` and the same title to the Java browser suite | done | story | traceability | missing |
