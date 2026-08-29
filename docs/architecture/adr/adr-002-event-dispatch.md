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
- Negative: no transactional outbox, no retries — acceptable for in-memory persistence. When a database adapter
  arrives, `ChannelIntegrationEventPublisher` is the place to swap in an outbox.
