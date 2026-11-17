---
story: place-the-order-from-the-review-and-see
epic: guest-checkout
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — place-the-order-from-the-review-and-see

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | test | tests/DcaShop.E2eTests/PlaceTheOrderE2eTest.cs:61 | minor | introduced |  |  | The browser test's title "Guest places the order" has no scenario in the shared specification's `scenarios.md`, so `SharedScenariosTest` fails with the specification switch on (plan's open assumption, same as `choose-how-to-pay`) | Add the scenario to the specification under this title, or bind the test to `scenario.checkout.guest-completes`'s title, and port it to the Java suite | done | story | traceability | missing |
