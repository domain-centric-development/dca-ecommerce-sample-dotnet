---
story: log-in-and-log-out
epic: account
---

# Findings — log-in-and-log-out

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | plan | tests/DcaShop.E2eTests/LoginE2eTest.cs:20 | minor | The title "Login opens the cart" is not in the shared specification's `scenarios.md`, so `SharedScenariosTest` fails when run with `-p:SpecificationPath` (the plan's open assumption) | Add the scenario to the specification and give the Java suite a test with the same title | open |
