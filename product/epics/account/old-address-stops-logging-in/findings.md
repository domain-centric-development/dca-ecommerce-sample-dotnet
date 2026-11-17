---
story: old-address-stops-logging-in
epic: account
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (5 rows, 0 without a fitting value)
---

# Findings — old-address-stops-logging-in

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Account/Adapter/Outgoing/Persistence/InMemoryAccountRepository.cs:77 | minor | introduced |  |  | delete releases the live instance's current address, not the one in `_indexedEmail`, unguarded — two rules for one index | `_indexedEmail.TryRemove(id, out var indexed)` then `_byEmail.TryRemove(KeyValuePair.Create(indexed, id))` | done | code | algorithm | incorrect |
| 2 | dca | tests/DcaShop.E2eTests/ProfileE2eTest.cs:135 | minor | introduced |  |  | DisplayName "Old address no longer logs in" has no scenario in the specification's `scenarios.md`; `SharedScenariosTest` fails with the specification switch on | add the scenario to the specification and to the Java browser suite under the same title (the user owns the specification) | done | story | traceability | missing |
| 3 | dca | tests/DcaShop.UnitTests/Account/AccountUniquenessTest.cs:1 | minor | introduced |  |  | the adapter's re-indexing is pinned only by the browser test; no repository test saves the same changed instance and looks up the old address | add a test: save, `ChangeEmail`, save the same instance, old address finds nothing and can be claimed by another account | done | test | checking | missing |
| 4 | dca | src/DcaShop.Account/Adapter/Outgoing/Persistence/InMemoryAccountRepository.cs:9 | minor | introduced |  |  | twin divergence: Java stores a reconstituted copy, .NET keeps the shared instance plus `_indexedEmail`; nothing records it | one sentence in ADR-001 or the porting log naming the different mechanism | open | docs | documentation | missing |
| 5 | clean-code | src/DcaShop.Account/Adapter/Outgoing/Persistence/InMemoryAccountRepository.cs:18 | minor | introduced |  |  | `_indexedEmail` is keyed by account but named unlike its siblings `_byEmail`/`_byLinkedUserId` | rename to `_indexedEmailByAccount` | done | code | naming | incorrect |
