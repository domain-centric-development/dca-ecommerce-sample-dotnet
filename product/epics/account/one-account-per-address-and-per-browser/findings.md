---
story: one-account-per-address-and-per-browser
epic: account
---

# Findings — one-account-per-address-and-per-browser

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | plan | tests/DcaShop.E2eTests/RegistrationRefusalE2eTest.cs:12 | minor | The browser test's DisplayName "Taken address is refused" is a new end-user test; the plan's open assumption says it must become a scenario of the shared specification, which this story did not do, so `SharedScenariosTest` (with `SpecificationPath`) will name it as a test without a scenario | Add the scenario to the shared specification's `scenarios.md` and the same-titled test to the Java suite | open |
