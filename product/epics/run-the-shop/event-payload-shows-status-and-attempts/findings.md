---
story: event-payload-shows-status-and-attempts
epic: run-the-shop
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (3 rows, 0 without a fitting value)
---

# Findings — event-payload-shows-status-and-attempts

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Backoffice/Adapter/Outgoing/Persistence/OutboxEventPublicationLogStore.cs:58 | minor | introduced |  |  | When a publication has several consumers, `Tries` adds the failed deliveries of all its consumers to one success for the publication as a whole. Two consumers that each fail once read `attempts: 3`, while Java's per-listener rows read 2. Nothing records this difference. | The user decides the unit (per delivery, per round, or the most-tried consumer). Record it in the store's `<remarks>` and in the glossary. | done | docs | documentation | missing |
| 2 | dca | tests/DcaShop.E2eTests/BackofficeE2eTest.cs:106 | minor | introduced |  |  | The specification's `scenarios.md` has no scenario with the title "Payload starts with status and attempts", so `SharedScenariosTest` fails when the specification is switched on. | Add the scenario to the specification and the same-titled test to the Java suite. This is the user's call. | done | story | traceability | missing |
| 3 | clean-code | tests/DcaShop.E2eTests/Pages/BackofficeEventsPage.cs:100 | minor | introduced |  |  | `ExpandCompletedPayloadOfAsync` also returns the payload's lines, which its name does not say. | Rename it to `CompletedPayloadLinesOfAsync`. | done | test | naming | incorrect |
