---
story: log-in-and-log-out
epic: account
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — log-in-and-log-out

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | plan | tests/DcaShop.E2eTests/LoginE2eTest.cs:20 | minor | introduced |  |  | The title "Login opens the cart" is not in the shared specification's `scenarios.md`, so `SharedScenariosTest` fails when run with `-p:SpecificationPath` (the plan's open assumption) | Add the scenario to the specification and give the Java suite a test with the same title | done | story | traceability | missing |
