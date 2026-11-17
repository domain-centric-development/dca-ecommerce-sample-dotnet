---
story: moving-between-pages-is-animated
epic: fits-its-surroundings
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — moving-between-pages-is-animated

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.E2eTests/PageTransitionE2eTest.cs:29 | minor | introduced |  |  | DisplayNames at :29, :54, :68 have no scenario in the shared specification's `scenarios.md`; `SharedScenariosTest` fails on them with `-p:SpecificationPath` (tests.md note) | add the three titles as scenarios to the specification and the Java suite | done | story | traceability | missing |
