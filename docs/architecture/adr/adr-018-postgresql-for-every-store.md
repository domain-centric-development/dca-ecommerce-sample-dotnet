# ADR-018: Every store on PostgreSQL — the outbox in the use case's transaction, in-memory stores as test fakes

Date: 2026-11-09 · Status: Accepted — supersedes the storage half of [ADR-009](adr-009-outbox-transactional-eligibility.md)
(the `Staged` state and the modeled commit gate); ADR-009's per-consumer completion, retry and replay stand.
Amends [ADR-004](adr-004-transaction-boundary.md) and [ADR-014](adr-014-uniqueness-is-claimed-in-the-store.md).

## Context

Every context kept its data in a `ConcurrentDictionary`. The stores handed out the aggregates they held, and
`InMemoryTransactionBoundary` undid nothing a use case had changed: only the outbox enlisted in its modeled commit.
A test passing in memory could therefore assume a rollback a real store gives — or miss one it would not. A stopped
process lost every account, cart and undelivered publication. The Java twin moved to PostgreSQL in the same work
package (its ADR-048); two databases would let the shops diverge exactly where they must agree, in how a cart's
checkout or a product's stock is locked.

## Decision

1. **PostgreSQL for every store.** Each repository and each processed-events store is a Dapper adapter over Npgsql
   (`Dapper<Aggregate>Repository`, `DapperProcessedEvents`): SQL written by hand, mapping in the adapter through the
   aggregate's `Reconstitute`, the same tables and stored spellings as the Java shop's. Uniqueness claims are
   constraints — the SKU, an account's email and linked identity, the active cart as the partial unique index
   `uq_carts_active_customer` — and the adapter translates the one violation its port declares.
2. **DbUp migrates the schema at start-up** (`DatabaseMigration`, registered as the first hosted service), from the
   embedded scripts under `src/DcaShop.Infrastructure/Persistence/Migrations`. Their names follow Flyway's
   (`V1__account.sql` …) and V1–V7 are the Java shop's files; V8 (Checkout's processed events) and V9 (the outbox)
   exist only here, because Checkout records a processed event here and Java's outbox is Spring Modulith's own table.
3. **One database transaction per use case.** `PostgresTransactionBoundary` opens it, makes it current for the
   asynchronous flow that opened it (`Database`, an `AsyncLocal`), and commits or rolls it back; repositories,
   stores and the outbox write in it without being handed it. Nested blocks join; an inner failure or refusal marks
   it rollback-only. After-commit and after-rollback hooks run outside the finished transaction. Only the boundary
   begins and ends a transaction (`DCA-LAY-004`).
4. **The outbox is two tables** (`event_publications`, `event_publication_deliveries`). A publication is inserted in
   the use case's transaction, so it exists exactly when the use case's work committed; the `Staged` state and the
   enlisted commit gate go, and `ITransactionHooks` keeps only the after-commit wakeup and the after-rollback hook.
   Delivery state changes under a row lock on the publication. The dispatcher reads every pending publication when it
   starts, so what a stopped process left behind is delivered by the next one.
5. **Check-then-change under a row lock.** A stock level read by product, and a checkout session read by its cart,
   are read `FOR UPDATE` inside a transaction (several products in product order). `InStockLevelsAsync` and
   `InCartSessionAsync` stay as the in-process serialization the Java shop keeps too.
6. **In-memory stores are test fakes.** They live in `tests/DcaShop.UnitTests/Fakes`, store and hand out copies, and
   serve the unit tests; the outbox fake only records.
7. **Tests run PostgreSQL in a container** (Testcontainers): one container per test run, a database migrated once as
   a template, and a copy of it for every host a test starts (`ShopFactory`), so xUnit's parallel classes share no
   data. Locally, `just run` starts the compose service `postgres` on port 5433.
8. **The seeder fills an empty catalogue only**, and waits for the deliveries its own seeding caused.

## Consequences

- Docker or Podman is required for the integration and browser suites, also in the factory's gates.
- A restarted process keeps its data. The scenarios that speak of a restart keep their meaning in the tests: the
  restarted shop is a second host on a database of its own.
- Cross-process serialization of a checkout *start* — before any session row exists to lock — is not covered by the
  row lock; `InCartSessionAsync` covers it within one process. The Java shop has the same gap.
- The cart's specification push-down is not adopted: `FindByAsync` still filters in memory over `FindAllAsync`,
  where the Java adapter translates the specification into SQL.
- Work started inside a transaction and left running inherits it through the `AsyncLocal`; such work is started
  from an after-commit hook.

## Evidence

`DapperRepositoriesTest` (round trips, the translated constraints, the stock row lock — red without `FOR UPDATE`, a
processed-event mark rolling back with its effect), `PostgresTransactionBoundaryTest` (the boundary's rules against
real writes), `RetainedDeliveryTest`, `OwnDomainEventDeliveryTest` and `IntegrationEventDeliveryTest` (the outbox on
PostgreSQL: an uncommitted publication is invisible to the dispatcher, a rolled-back one is gone).
