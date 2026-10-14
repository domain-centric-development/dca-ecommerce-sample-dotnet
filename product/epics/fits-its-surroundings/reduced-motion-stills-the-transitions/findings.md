---
story: reduced-motion-stills-the-transitions
epic: fits-its-surroundings
---

# Findings — reduced-motion-stills-the-transitions

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.E2eTests/Pages/BasePage.cs:134 | minor | empty `catch { }` hides that a skipped transition rejecting `ready` is expected | add a one-line why-comment inside the catch | done |
| 2 | clean-code | tests/DcaShop.E2eTests/Pages/BasePage.cs:139 | minor | `getComputedTiming().duration` computed twice (filter and map) | map once to `{ part, duration }`, then filter and format | done |
| 3 | dca | tests/DcaShop.E2eTests/ReducedMotionE2eTest.cs:75 | minor | title "Reduced motion means no animation" has no scenario in the shared specification's `scenarios.md`; `SharedScenariosTest` fails on it when run with `SpecificationPath` (same pre-existing gap as the other appearance tests) | the user adds the scenario to the shared specification when the Java twin lands, or exempts appearance stories | open |
