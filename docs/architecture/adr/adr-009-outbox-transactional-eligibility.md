# ADR-009: Outbox eligibility is transactional; completion is per consumer

Date: 2026-09-09 · Status: Accepted

## Context and decision

A registered publication used to be replayable before its transaction committed. An after-commit release flag
also cannot establish eligibility safely: the process can stop after commit and before that callback.
`Register` now captures an immutable serialized payload as `Staged`. The boundary enlists a commit participant;
under the modeled commit gate it changes the publication to `Pending`. After-commit hooks only wake the dispatcher.
Rollback removes the staged publication. A failed later commit participant rolls back eligibility before readers
can observe it. Failure of a wakeup after successful commit cannot undo eligibility. A reconstructed dispatcher
reads retained `Pending` state even without a `Release` callback. Retained here means the same in-memory store;
a new process loses that store. This is a transactional state-machine stand-in, not durable database storage.

The boundary models only enlisted participants. Existing live-object in-memory repositories are not a database
transaction and do not universally undo arbitrary aggregate mutation. The containment test enlists a modeled
aggregate participant and checks it together with real outbox capture; Java's JDBC counterpart tests real storage.
Replacing the storage requires aggregate and publication changes to join the same durable database transaction.

Each event captures a consumer list on first dispatch and tracks immutable per-consumer completion. A retry touches
only incomplete work. Automatic delivery permits five attempts, with 200 ms exponential backoff (400/800/1600 ms
subsequent delays); terminal consumers remain `Failed`. Backoffice shows Staged/Pending/Completed/Failed, attempts,
consumer identity, next due time and last error. Its authenticated, antiforgery-protected POST replay resets only
failed consumers to a new bounded retry run. Completed consumers, event identity and serialized payload remain intact.

Delivery identity is event id + stable consumer id + effect discriminator. Consumer ids are implementation type names;
renaming a consumer with retained publications requires a migration. Provider acceptance counts as success. A crash
between acceptance and local acknowledgement retries with the same key. A provider honoring it produces one effect;
a provider without idempotency may produce two. Local completion bookkeeping cannot remove that risk. The snapshot
is event content; rendering version and recipient capture/resolution must be decided for each concrete effect.
No email provider, broker, inbox ledger or new effect contract is introduced.

## Evidence and harness

`OutboxEligibilityTest`, `RetainedDeliveryTest`, and `IntegrationEventDeliveryTest` cover uncommitted/rollback,
commit with and without wakeup, failed commit participant, post-commit wakeup failure, per-consumer retry, terminal
failure/manual recovery, retained snapshot and supported/unsupported provider idempotency. USE-012 only checks static
boundary evidence: a publish after an empty boundary in the same method passes; runtime containment is separate.
Catalog: generic event-delivery decision/note/recipe and transaction pitfall updated from authored sources.
Rules: USE-012 ported, USE-009 conservative optional-events exception. Marker: none.
