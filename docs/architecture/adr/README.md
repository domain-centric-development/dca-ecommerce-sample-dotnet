# Architecture Decision Records

| ADR | Title | Status |
|---|---|---|
| [ADR-001](adr-001-solution-layout.md) | One project per bounded context, layers as folders | Accepted |
| [ADR-002](adr-002-event-dispatch.md) | In-process domain events, outbox-based integration events, no MediatR | Accepted |
| [ADR-003](adr-003-pattern-selection-per-context.md) | Pattern selection per context and stage-1 stand-ins | Accepted |
| [ADR-004](adr-004-transaction-boundary.md) | Transaction boundary via the `IUnitOfWork` port, remote calls outside it | Accepted |
