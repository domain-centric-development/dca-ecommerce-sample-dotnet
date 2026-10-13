---
story: retry-a-failed-delivery
epic: run-the-shop
---

# Findings — retry-a-failed-delivery

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.E2eTests/BackofficeRetryE2eTest.cs:34 | minor | The browser test "Retry completes a failed delivery" has no scenario in the shared specification's `scenarios.md`, so `SharedScenariosTest` with the specification switch names it (tests.md notes says the same; "Failed entry offers a retry" at :21 had this gap before the story) | Add both scenarios to the specification (the user owns it) and take them into the Java suite under the same titles | open |
