# ADR-014: Uniqueness That Spans Aggregates Is Claimed in the Store, Not Checked Before It

**Date**: 2026-09-20 · **Status**: Accepted

## Context

A customer has at most one active cart, an email address belongs to one account, a SKU names one product. None
of those is an invariant of a single aggregate, so the use cases asked first and wrote afterwards. Between the
question and the write nothing holds: two requests for the same customer both find no active cart and both
create one. This sample has only in-memory repositories, so nothing else catches it either. The Java sample
carried the same shape and is fixed alike (its ADR-042).

## Decision

- `InMemoryShoppingCartRepository` keeps an active-cart-per-customer index and claims it in `SaveAsync` with
  `GetOrAdd`; a second active cart is refused with `InvalidOperationException`. `FindActiveByCustomerAsync` reads
  the index instead of scanning.
- `InMemoryProductRepository` and `InMemoryAccountRepository` claim the SKU, the email address and the linked
  user id the same way rather than overwriting their index entries.
- `GetOrCreateActiveCartUseCase` catches the refusal and returns the cart that won the race — what a caller does
  with a constraint violation from a database.
- The checks before the save stay: they give the ordinary caller a clear answer, the claim makes the rule true
  when two arrive together.

## Consequences

- Positive: the adapter teaches the contract a relational store has. `ActiveCartUniquenessTest`,
  `SkuUniquenessTest` and `AccountUniquenessTest` hold it, with a concurrency case of eight simultaneous requests
  over twenty-five rounds; removing the claim fails them on every run.
- Negative: a repository that refuses a save carries a rule. Deliberate — the rule is the store's — but it is one
  more place to look for what can fail.
- Neutral: `InvalidOperationException` is the general-purpose refusal both samples use; a domain exception model
  is a separate open question.
