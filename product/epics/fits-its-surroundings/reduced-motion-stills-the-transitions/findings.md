---
story: reduced-motion-stills-the-transitions
epic: fits-its-surroundings
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (3 rows, 0 without a fitting value)
---

# Findings — reduced-motion-stills-the-transitions

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.E2eTests/Pages/BasePage.cs:134 | minor | introduced |  |  | empty `catch { }` hides that a skipped transition rejecting `ready` is expected | add a one-line why-comment inside the catch | done | test | comment | missing |
| 2 | clean-code | tests/DcaShop.E2eTests/Pages/BasePage.cs:139 | minor | introduced |  |  | `getComputedTiming().duration` computed twice (filter and map) | map once to `{ part, duration }`, then filter and format | done | test | duplication | extraneous |
| 3 | dca | tests/DcaShop.E2eTests/ReducedMotionE2eTest.cs:75 | minor | introduced |  |  | title "Reduced motion means no animation" has no scenario in the shared specification's `scenarios.md`; `SharedScenariosTest` fails on it when run with `SpecificationPath` (same pre-existing gap as the other appearance tests) | the user adds the scenario to the shared specification when the Java twin lands, or exempts appearance stories | done | story | traceability | missing |
