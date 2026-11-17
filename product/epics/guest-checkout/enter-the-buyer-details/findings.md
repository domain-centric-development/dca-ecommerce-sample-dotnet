---
story: enter-the-buyer-details
epic: guest-checkout
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — enter-the-buyer-details

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | adopt | tests/DcaShop.E2eTests/EnterBuyerDetailsE2eTest.cs:40 | minor | introduced |  |  | "Not reached" reads the navigation log right after the click with nothing awaited; the clause holds through :39 (an invalid form is never submitted) | Wait for the network to settle before asserting the absent navigation | done | test | timing | incorrect |
| 2 | plan | tests/DcaShop.E2eTests/EnterBuyerDetailsE2eTest.cs:12 | minor | introduced |  |  | DisplayNames at :12 and :22 have no scenario in the shared specification; `SharedScenariosTest` fails with `-p:SpecificationPath` | Add both scenarios to the specification's `scenarios.md` and the Java suite under the same titles | done | story | traceability | missing |
