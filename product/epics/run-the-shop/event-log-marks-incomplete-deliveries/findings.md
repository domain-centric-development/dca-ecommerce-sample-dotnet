---
story: event-log-marks-incomplete-deliveries
epic: run-the-shop
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — event-log-marks-incomplete-deliveries

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Backoffice/Adapter/Incoming/Web/EventPublicationPageViewModel.cs:53 | minor | introduced |  |  | "may be retried" is decided in the adapter by matching `summary.Status` against a re-typed `"Failed"`, while the sibling `IsCompleted` is stated by the result (`GetEventPublicationsResult.cs:43`); the coupling to the outbox enum name was there before, only moved | Give `EventPublicationSummary` an `IsFailed` beside `IsCompleted` and map it in `Item.From`; drop `FailedStatus` | done | code | domain-concept | incorrect |
| 2 | clean-code | src/DcaShop.Web/Views/Backoffice/Events.cshtml:43 | minor | introduced |  |  | The completed branch writes the literal `Completed` while `StatusLabel` now also yields "Completed"; the badge text is decided in two places | Render `@item.StatusLabel` in both branches, the `if` choosing only the CSS modifier | done | code | duplication | extraneous |
