---
story: the-visitor-chooses-the-shops-look
epic: fits-its-surroundings
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — the-visitor-chooses-the-shops-look

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | plan | tests/DcaShop.E2eTests/ThemeChoiceE2eTest.cs:12 | minor | introduced |  |  | Five new E2E display names have no scenario in the shared specification's `scenarios.md` nor a twin in the Java suite; `SharedScenariosTest` fails on them once `SpecificationPath` is set | Add the five scenarios to the shared specification and port the tests to the Java E2E suite under the same titles | done | story | traceability | missing |
