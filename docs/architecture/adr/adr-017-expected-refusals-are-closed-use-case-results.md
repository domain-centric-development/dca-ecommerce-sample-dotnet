# ADR-017: Expected Refusals Are Closed Use-Case Results

**Date**: 2026-11-07 · **Status**: Accepted · amends ADR-016 · the boundary below is `PostgresTransactionBoundary` since [ADR-018](./adr-018-postgresql-for-every-store.md), with the same contract

## Context

ADR-016 gave every failure its own type and made the incoming adapter the one place that turns a type into an
answer. Every refusal still travelled as an exception, which left three gaps:

- **The seam was a convention, not a type.** An adapter test stubbed the input port to throw a refusal; nothing but
  a paired test held that the real use case threw the same type. A renamed or new refusal compiled everywhere and
  surfaced as a `500`.
- **The port did not say how it refuses.** `ICreateCartInputPort.ExecuteAsync` returned the success; which refusals
  a caller had to answer was in the XML docs or nowhere.
- **Expected outcomes took the exceptional path.** "That stock keeping unit is already in use" is an ordinary
  answer of the use case, not an exceptional state; it was an exception because that was the one channel.

Two contexts had already started a second channel: Account answered a login, a password change and a profile change
with a `Success` flag or an outcome enum, Inventory a reservation with an empty-or-not list of shortages — each
open, none checked by the compiler.

## Decision

**A use case returns every expected refusal as a case of a closed result.** Expected means a refusal the caller
answers: not found, not owned, a value the request names that the shop does not offer, a rule of the model the
caller can act on. Everything else stays an exception: a value object's argument guard (`ArgumentException`), a rule
of the model no use case names as a case of its own (`DomainException`), a failure of the infrastructure. The style
is chosen per bounded context and is uniform within it — no context mixes a returned refusal with a thrown one. In
this shop every context returns; the method holds both styles as equal.

- **Shape.** `[GenerateOneOf] public sealed partial class <Op>Result : OneOfBase<<Op>Result.A, …>` with one nested
  `sealed record` per case: the success named for what happened (`Added`, `Created`, `Submitted`, `Confirmed`,
  `Found`), each refusal named like the failure it replaces without `Exception` (`CartNotFound`, `DuplicateSku`,
  `CheckoutStepOutOfOrder`). A case carries ids and figures, never display text. A use case simply `return`s a case
  (OneOf's implicit conversion).
- **Every refusal case implements `IRefusal`** (`DomainCentric.BuildingBlocks.Application`). `DCA-ERR-008`
  (experimental, enforced) holds that each closed result an input port answers with has at least one such case; a
  result that refuses nothing stays a plain record.
- **A returned refusal rolls back like a thrown failure.** `InMemoryTransactionBoundary` looks at the value the work
  returns: when it is an `IRefusal`, or a OneOf result whose current case is one, the outermost block rolls back and
  returns the result unchanged, without throwing. Nested, a refusal marks the shared transaction rollback-only; an
  outermost block that returns a refusal rolls back quietly, also after a caught inner failure; one that returns a
  success after an inner refusal throws `TransactionRolledBackException`. A database boundary keeps the same contract.
- **Decide, then write, stays the rule.** Every refusal is decided before the first write; the rollback is the net
  for a use case that does not. `StartCheckout` superseded and saved the previous session before the model could
  refuse an empty checkout; it now starts the new session first, as the Java twin does, so no refusal in this shop
  follows a write. A remote effect is no write the net reaches: `ConfirmCheckout` keeps giving the stock
  reservation back for a refusal that follows it (ADR-013), as it did for a thrown failure.
- **The domain keeps throwing.** An aggregate's refusal is a `DomainException`; the use case catches the named type
  once, narrowly, before it writes anything, and returns its case.
- **The failure hierarchy names the port's direction.** `PortException` is the abstract base of every failure a
  port's contract declares; `InputPortException` (with `UseCaseException`, thrown only by a use case) and
  `OutputPortException` derive from it. A failure an output port declares derives from `OutputPortException`
  beside the port, the adapter throws it, the use case catches it and returns its case:
  `ActiveCartAlreadyExistsException` (`CreateCart` catches it inside its transaction) and the payment and mail
  providers' `PaymentProviderUnavailableException`, `PaymentInitiationFailedException` and
  `MailProviderUnavailableException` (amendment below) are the ones in this shop. No incoming adapter catches `PortException`, `InputPortException` or `OutputPortException`
  generically.
- **The adapter matches.** Each translation site calls `Match`/`Switch` with one function per case and words the
  answer as before; a `*Problem` action result per API context writes the problem document, shared with the
  context's `*ApiExceptionHandler`, which keeps only what is still thrown. A caller with nobody to answer — an event
  consumer, the sample-data seeder, Cart's open host service for a completion — turns a refusal back into a thrown
  `InvalidOperationException`, so a delivery is retried as before.
- **OneOf is the bridge, C# 15 `union` the target.** C# 15 `union` types with an exhaustive `switch` need the .NET 11
  SDK, not released for the LTS line the samples run on. Until then the results stand on OneOf 3.0.271 and its
  source generator, pinned as `<OneOfVersion>` in `Directory.Build.props`; `ConfirmCheckoutResult`'s eleven cases
  need `OneOf.Extended`, OneOf's own bases stop at nine. Once the samples run .NET 11, each result becomes a `union`
  of the same cases: one line per type, and each `Match` a `switch` expression.

The Java sample decided the same in its ADR-047; both samples carry the same result and case names where the
contexts match.

## Consequences

Measured over the six contexts converted after the Cart prototype (Account, Checkout, Pricing, Product, Inventory,
Backoffice); code lines without blanks, comments and `using`s:

| Measure | Before | After |
|---|---|---|
| lines at the translation sites | 1309 | 1479 (+13 %) |
| application exception classes | 16 | 0 |
| domain exception classes | 18 | 18 |
| exception-handler arms (API) | 14 | 7 |
| `Match`/`Switch` calls in the contexts | 0 | 25 |
| `catch` clauses in the application layers | 6 | 23 |
| application `throw`s of a refusal | 24 | 0 |

With the Cart prototype the shop has 26 closed results and one application exception left
(`ActiveCartAlreadyExistsException`, an `OutputPortException`); the amendment below adds `GetCheckoutSession` (27)
and three `OutputPortException`s of the foreign systems' ports. The HTTP answers of every API refusal — status,
content type, problem body, `WWW-Authenticate` — are byte for byte those of the commit before the prototype.

- **What it buys.** A new case breaks every `Match` that must answer it, at compile time; before, a new exception
  compiled everywhere and became a `500`. The seam an adapter test stubbed is the compiler's now. No seam pair became
  redundant: the adapter tests stub a case where they used to throw one.
- **The translation grew.** The table moved from one exception handler into a `Match` at each call site, plus a
  `*Problem` class per API context (see below). Checkout grew most (+22 %): its step pages map each result onto an
  adapter-side `StepRefusal` that `CheckoutStepPage` words once.
- **The application gained catches.** Domain refusals are still thrown and caught once per use case — Checkout's
  step use cases catch two to four session refusals each, `ConfirmCheckout` six through one helper.
- **Two styles per adapter remain.** The `Match` for the cases, a catch or handler arm for the residue
  (`ArgumentException`, `DomainException`).
- **OneOf's exhaustiveness is by arity.** The functions are positional: two of the same shape can be swapped and
  compile, and a missing one is reported as a missing argument, not by the case's name. `union` closes that gap.
- **Async `Match` is noisy.** `Match<Task<ActionResult<T>>>` and a `Task.FromResult` per refusal; implicit
  conversions do not flow through generic inference, so `InTransactionAsync<XResult>` and
  `Task.FromResult<XResult>(…)` are spelled out.
- **Two write paths for one problem document.** A problem written from an action carries `; charset=utf-8`, one from
  the exception handler does not; each API context's `*Problem` writes the action's answer itself so both read the
  same. It is the same 35–57 lines four times, because no project below the contexts may reference ASP.NET Core.
- **A result can be ignored.** C# has no must-use check: a caller that drops the result swallows the refusal. Setup
  helpers in the tests assert the success case for that reason.
- **Not every result became closed.** A query that answers "which one, if any" keeps its nullable answer
  (`GetActiveCheckoutSession`, `GetConfirmedCheckoutSession`, `ResumeCheckout`); `ReduceStock` reports a failed line inside a sale that stands — a line failure must not roll the
  other lines back, so it is no refusal of the request.
- **The build needs the unreleased libraries** for `IRefusal`, the port hierarchy and `DCA-ERR-008`
  (`-p:UseLocalDcaDotnet=true`); the pre-commit hook builds against the published packages, so these commits went in
  with `--no-verify` until the libraries are released.
- **Harness questions.** Catalog: a decision node (thrown or returned, per context) and a recipe for a use case with
  a closed result, both carried by WP-171. Rule: `DCA-ERR-008` new and experimental; `DCA-ERR-006` counts an
  exhaustive `Match` over a closed result as translation. Marker: `IRefusal`, and the `PortException` hierarchy.

## Amendment (2026-11-07): foreign systems, and "which one, if any"

Taken while aligning the checkout with the Java twin (WP-171); the twin's ADR-047 carries the same amendment.

- **A foreign system's expected failure is a case; the system's own infrastructure throws.** The payment provider
  and the mail provider are external systems on the context map. Their output ports declare what the system may
  answer besides success as `OutputPortException`s — `IPaymentProvider.InitiatePaymentAsync` answers the payment
  reference and throws `PaymentInitiationFailedException` (the provider refused) or
  `PaymentProviderUnavailableException` (no answer in time, no connection, an answer outside the contract);
  `IOrderConfirmationMailer.SendAsync` throws `MailProviderUnavailableException`. The adapter throws them, the use
  case catches them and returns `PaymentInitiationFailed`, `PaymentProviderUnavailable` or `MailProviderUnavailable`,
  each an `IRefusal`. The port's earlier outcome values (`PaymentOutcome.Unavailable` on the initiation, `MailOutcome`)
  are gone; confirming and cancelling a payment keep `PaymentResult`, since no use case answers them. A failure of
  the shop's own store stays an exception.
- **`SendOrderConfirmation` is `Sent | NoPlacedOrder | MailProviderUnavailable`** in both shops. `NoPlacedOrder`
  replaces `CheckoutSessionNotFound` and `BuyerInfoNotSubmitted`: a session the caller does not have and one without
  buyer info hold no placed order, and nothing can be sent for either, now or later. The event consumer has nobody to
  answer: it parks `NoPlacedOrder` (logged, completed — trying again changes nothing; it used to fail the delivery)
  and throws `MailProviderUnavailable` back, so the outbox retries the delivery.
- **"Which one, if any" queries stay plain; a query by id refuses.** When "none" is a normal answer — the customer's
  active checkout, their placed order, an open checkout of their cart to resume — absence is an empty value:
  `GetActiveCheckoutSession`, `GetConfirmedCheckoutSession` and `ResumeCheckout` keep their nullable answers. A query
  that names one session by its id demands that session: `GetCheckoutSession` is now `Found | CheckoutSessionNotFound`,
  as `GetReview` already was, and the step page turns `CheckoutSessionNotFound` into the way back to the cart that
  the use case used to put in its step access. Pages answer as before.
