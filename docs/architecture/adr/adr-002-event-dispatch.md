# ADR-002: In-Process Domain Events, Channel-Based Integration Events, No MediatR

**Date**: 2026-08-30 · **Status**: Accepted

## Context

The Java sample relies on Spring's `ApplicationEventPublisher`: `@EventListener` for synchronous relays inside a
context and `@ApplicationModuleListener` for asynchronous, after-commit delivery across contexts. .NET has no
built-in counterpart. MediatR is the common substitute, but it introduces a framework type between use case and
port and blurs the distinction between commands and events.

## Decision

- The `IDomainEventPublisher` port is implemented by `InProcessDomainEventPublisher` (shared kernel adapter): it
  dispatches each event synchronously to all registered `IEventListener`s in the current DI scope and clears the
  aggregate. This is the relay point where outgoing event adapters (`*EventPublisher`) translate domain events into
  integration events.
- The `IIntegrationEventPublisher` port is implemented by `ChannelIntegrationEventPublisher`, writing to an
  unbounded `System.Threading.Channels` channel. `IntegrationEventDispatcherService` (a `BackgroundService` in
  `DcaShop.Infrastructure`) drains it and delivers each event in its own DI scope — asynchronous and after the
  publishing use case returned, which is the semantic `@ApplicationModuleListener` gives the Java sample.
- Listeners are matched by **assignability**, so a consumer listening on its own interface
  (`ICartCompletionTrigger`) receives the producer's event that implements it — the interface-inversion pattern of
  the Java sample without Spring Modulith.
- No MediatR: ports stay explicit interfaces from the building blocks; nothing framework-specific enters the
  application layer.

## Consequences

- Positive: two small classes replace a framework; the event flow is readable in one file each; tests can assert
  eventual consistency by polling the consuming context.
- Negative — **this event infrastructure is deliberately non-durable and must not be read as a production
  pattern**:
  - The channel is unbounded and memory-only: a process restart loses every queued integration event.
  - A failing consumer is logged and the event is dropped — no retry, no dead-letter queue.
  - Persistence and publication are not atomic: `SaveAsync` can succeed and the subsequent publish can fail
    (the same save-then-publish gap the Java sample has; only Spring Modulith's event publication registry
    persists the *integration* leg there).

  It teaches the *shape* of the flow — domain event → outgoing event adapter → integration event → consumer in
  another context — with the least possible machinery. A production implementation replaces
  `ChannelIntegrationEventPublisher` with a transactional outbox written in the same unit of work as the
  aggregate, a bounded channel or broker for backpressure, retries with dead-lettering in the dispatcher, and
  idempotent consumers (`EventId` is already on every event for exactly that). The ports and the use cases do not
  change when that happens; only the two adapters and the background service do.
