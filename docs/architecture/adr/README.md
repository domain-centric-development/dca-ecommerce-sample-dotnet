# Architecture Decision Records

| ADR | Title | Status |
|---|---|---|
| [ADR-001](adr-001-solution-layout.md) | One project per bounded context, layers as folders | Accepted |
| [ADR-002](adr-002-event-dispatch.md) | In-process domain events, outbox-based integration events, no MediatR | Accepted |
| [ADR-003](adr-003-pattern-selection-per-context.md) | Pattern selection per context and stage-1 stand-ins | Accepted |
| [ADR-004](adr-004-transaction-boundary.md) | Transaction boundary via `ITransactionBoundary`, remote calls outside it | Accepted |
| [ADR-005](adr-005-antiforgery-and-safe-methods.md) | Antiforgery token on every writing form, no state-changing `GET` | Accepted |
| [ADR-006](adr-006-identity-and-session-cookies.md) | Two cookies for identity and session, signed by an own JWT middleware | Accepted |
