# ADR-013: Local Preconditions Come Before a Remote Effect, and an Unusable Effect Is Released

**Date**: 2026-09-17 · **Status**: Accepted

## Context

`SubmitPayment` read the session total, called `InitiatePaymentAsync(...)` on the payment provider, and only then
opened the short transaction in which `session.SubmitPayment(...)` may still refuse the session — because it is
not active, because the buyer or delivery step is missing, because the session has moved past payment. A payment
intent could therefore exist at the provider for a checkout this shop then rejected: the shopper sees an error,
and a reservation sits with the provider that nobody will use or release. [ADR-004](adr-004-transaction-boundary.md) keeps the remote call out of
the transaction; it says nothing about what must be true before the call is made at all. The Java sample carried
the identical ordering and is changed alike.

## Decision

- `CheckoutSession.AssertReadyForPayment()` — the preconditions `SubmitPayment` enforces plus a total that is not
  zero, changing nothing. The use case loads the session once, asks it, and only then reaches the provider; the
  amount comes from that same snapshot.
- When the transaction fails afterwards, the use case calls `CancelPaymentAsync(...)` for the reference it just
  created and re-throws the original failure. A refused cancellation is logged at warning level.
- Not an idempotency key: that needs a parameter on the port, and the redirect flow still to be built is where
  the port's contract is decided. Compensation uses the port as it stands and gives `CancelPaymentAsync` its
  first caller.

## Consequences

- Positive: no payment intent exists for a checkout that cannot accept it; `SubmitPaymentUseCaseTest` proves the
  order and the compensation and fails when either half is removed.
- Negative: the session is loaded twice, and compensation is best effort — a process that dies between
  initiation and cancellation orphans the intent. A real system needs reconciliation.
