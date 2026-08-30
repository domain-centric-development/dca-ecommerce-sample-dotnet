# ADR-004: Transaction Boundary via the `IUnitOfWork` Port, Remote Calls Outside It

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

- Writing use cases draw their boundary explicitly with the `IUnitOfWork` output port from the building blocks:
  remote-capable reads first, then `RunAsync(load, mutate, save, publish)`. Use cases without remote reads wrap
  their whole body. Read-only use cases run without a unit of work.
- `InMemoryUnitOfWork` (shared kernel adapter, scoped) implements the port for the in-memory stage: nested calls
  join the outer unit of work; `ITransactionHooks.AfterCommit` collects work that must become visible only on
  commit and drops it on rollback.
- `OutboxIntegrationEventPublisher` enlists the outbox registration through `AfterCommit`: a use case that throws
  after publishing leaves no integration event behind — the in-process equivalent of an outbox row written in
  the aggregate's transaction (ADR-002).
- Only transactional resources are used inside `RunAsync`: repositories, stores, the event publishers. Ports that
  may leave the process (`IArticleDataPort`, `ICartDataPort`, `IPaymentProviderRegistry`, …) are called before.
- `DCA-NET-006` keeps EF Core, `System.Data` and `System.Transactions` out of the application layer, so the only
  place that knows how a transaction is opened is the `IUnitOfWork` adapter.

## Consequences

- Positive: the boundary is visible in the code that owns it; the connection is held for microseconds, not for
  a remote round trip; swapping `InMemoryUnitOfWork` for an EF Core implementation (`DbContext` transaction,
  `SaveChangesAsync` on commit, outbox table) touches one adapter and no use case.
- Negative: every writing use case carries an `IUnitOfWork` dependency and a lambda. A decorator around
  `IUseCase<,>` would remove the boilerplate for use cases without remote reads, at the price of hiding the
  boundary; the sample prefers the explicit form so both shapes stay readable side by side.
