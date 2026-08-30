# ADR-002: In-Process Domain Events, Outbox-Based Integration Events, No MediatR

**Date**: 2026-08-30 · **Status**: Accepted

## Context

The Java sample relies on Spring's `ApplicationEventPublisher`: `@EventListener` for synchronous relays inside a
context and `@ApplicationModuleListener` for asynchronous, after-commit delivery across contexts. .NET has no
built-in counterpart. MediatR is the common substitute, but it introduces a framework type between use case and
port and blurs the distinction between commands and events.

## Decision

- The `IDomainEventPublisher` port is implemented by `InProcessDomainEventPublisher` (shared kernel adapter): it
  dispatches each event synchronously to all registered `IEventListener`s in the current DI scope and clears the
  aggregate **afterwards** — clearing is the acknowledgement that every listener has seen the event. A throwing
  listener fails the use case and leaves the events on the aggregate. This is the relay point where outgoing event
  adapters (`*EventPublisher`) translate domain events into integration events.
- The `IIntegrationEventPublisher` port is implemented by `OutboxIntegrationEventPublisher`: publishing registers an
  `IntegrationEventPublication` (event + delivery bookkeeping) in the `IIntegrationEventOutbox` — inside the use
  case's transaction; the publication is released to the dispatcher after commit and discarded after rollback
  (ADR-004).
  `IntegrationEventDispatcherService` (a `BackgroundService` in `DcaShop.Infrastructure`) reads due publications and
  delivers each in its own DI scope — asynchronous and after the publishing use case returned, which is the semantic
  `@ApplicationModuleListener` gives the Java sample. A failed delivery is recorded and retried with exponential
  backoff (`IntegrationEventRetryPolicy`, default 5 attempts); when the policy is exhausted the publication is marked
  `Failed` and stays inspectable. Outstanding publications are replayed when the dispatcher starts — the counterpart
  of Spring Modulith's event publication registry with `republish-outstanding-events-on-restart`.
- Delivery is therefore **at least once**; consumers are idempotent (`CompleteCartUseCase` returns early for an
  already completed cart). `EventId` is the idempotency key for consumers that need to remember what they processed.
- Listeners are matched by **assignability**, so a consumer listening on its own interface
  (`ICartCompletionTrigger`) receives the producer's event that implements it — the interface-inversion pattern of
  the Java sample without Spring Modulith.
- No MediatR: ports stay explicit interfaces from the building blocks; nothing framework-specific enters the
  application layer.

## Consequences

- Positive: two small classes replace a framework; the event flow is readable in one file each; tests can assert
  eventual consistency by polling the consuming context.
- Negative: the outbox is `InMemoryIntegrationEventOutbox` — durable within the process only; a restart loses
  outstanding publications, and the in-memory store cannot make the registration atomic with `SaveAsync` on the
  aggregate (it emulates the rollback through `AfterRollback`). Both gaps close with a database-backed outbox that
  writes the publication in the aggregate's transaction; the interface, the publisher, the dispatcher, the ports
  and the use cases stay as they are. Synchronous domain-event listeners
  run inside the use case, so a failure there is a failure of the use case — no outbox needed on that leg.
