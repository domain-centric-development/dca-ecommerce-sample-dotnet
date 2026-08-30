# ADR-004: Transaction Boundary via `ITransactionBoundary`, Remote Calls Outside It

**Date**: 2026-08-30 · **Status**: Accepted

## Context

A use case is the unit of work: load an aggregate, mutate it, save it, publish its events. That sequence must be
atomic once a database sits behind the repository. .NET has no declarative `@Transactional`; the boundary is
either a decorator around `IUseCase<,>` or drawn explicitly inside the use case.

Several use cases also read from other contexts or external systems before they mutate anything — the cart asks
the Product Catalog for the article, checkout asks the payment provider registry. In a distributed deployment
those reads are HTTP calls. A remote call inside a transaction holds the database connection for the round trip;
under load the pool runs dry, and a rollback cannot undo a remote effect. The boundary therefore has to be
smaller than the use case.

## Decision

- Writing use cases draw their boundary explicitly with `ITransactionBoundary` from the building blocks — an
  application-layer execution abstraction, deliberately not an output port (a transaction is no interaction with the
  outside world; it defines the execution semantics of several such interactions):
  remote-capable reads first, then `InTransactionAsync(load, mutate, save, publish)`. Use cases without remote reads wrap
  their whole body. Read-only use cases run without a transaction.
- `InMemoryTransactionBoundary` (shared kernel infrastructure, scoped) implements the abstraction for the in-memory
  stage: nested calls join the outer transaction; `ITransactionHooks` offers `AfterCommit` and `AfterRollback` for
  work that reacts to the outcome — not for work that belongs *into* the transaction.
- `OutboxIntegrationEventPublisher` registers the publication **inside** the transaction, next to the aggregate, so
  there is no window in which the aggregate is committed but the event is not recorded. `AfterCommit` only releases
  the publication to the dispatcher; `AfterRollback` discards it (a database outbox rolls the row back by itself).
  This is exactly the shape a database-backed outbox needs, so the swap touches the outbox store, not the
  publisher or the use cases (ADR-002).
- Only transactional resources are used inside `InTransactionAsync`: repositories, stores, the event publishers. Ports that
  may leave the process (`IArticleDataPort`, `ICartDataPort`, `IPaymentProviderRegistry`, …) are called before.
- `DCA-NET-006` keeps EF Core, `System.Data` and `System.Transactions` out of the application layer, so the only
  place that knows how a transaction is opened is the `ITransactionBoundary` implementation in infrastructure.

## Consequences

- Positive: the boundary is visible in the code that owns it; the connection is held for microseconds, not for
  a remote round trip; swapping `InMemoryTransactionBoundary` for an EF Core implementation (`DbContext` transaction,
  `SaveChangesAsync` on commit, outbox table) touches one adapter and no use case.
- Negative: every writing use case carries an `ITransactionBoundary` dependency and a lambda. A decorator around
  `IUseCase<,>` would remove the boilerplate for use cases without remote reads, at the price of hiding the
  boundary; the sample prefers the explicit form so both shapes stay readable side by side.
