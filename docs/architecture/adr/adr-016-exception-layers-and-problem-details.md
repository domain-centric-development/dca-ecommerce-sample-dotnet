# ADR-016: A Failure Carries Its Own Type, and Only the Adapter Turns It Into an Answer

**Date**: 2026-09-21 · **Status**: Accepted

## Context

Until now this shop raised the platform's own exceptions for everything. A cart that refused a change, a stock
keeping unit the catalog already held, a session that belonged to somebody else and a null argument all arrived at
the boundary as `ArgumentException` or `InvalidOperationException`. Measured before this change: 96 argument
throws, 62 invalid-operation throws, 40 of them inside `Domain` namespaces.

That cost us three things.

- **The adapter could not tell the cases apart.** `ProductResource` answered `400` with the exception message
  whether the stock keeping unit was malformed or already taken; `ShoppingCartResource` and `AuthResource` did the
  same with one `catch (Exception e) when (e is ArgumentException or InvalidOperationException)` each. A message is
  not a contract: a reworded sentence changes nothing the caller can branch on, and the status is the same either
  way.
- **A defect looked like a caller's mistake.** `InvalidOperationException` is also what the runtime raises when
  this application does something wrong internally. Catching it at the boundary and answering `400` reports our own
  bug as the customer's error, and hides it from anything watching for server faults.
- **The domain lost words it has.** "The reservation is already confirmed" and "only three left" are sentences a
  domain expert says. As argument exceptions they were strings.

`ChangePasswordUseCase` shows the seam at its clearest: it had to catch `ArgumentException` around a strength
check and carried a four-line comment explaining that the catch must not accidentally swallow a failure from the
hashing adapter, because both arrive as the same type.

## Decision

**Three layers of failure, each with a base type from `DomainCentric.BuildingBlocks`, and one translation site per
context.** The Java sample decided the same in its ADR-044; the two samples carry the same type names.

- A broken rule of the model is a `DomainException` in the context's `Domain` namespace:
  `InsufficientStockException`, `CartNotModifiableException`, `CheckoutStepNotCompletedException`,
  `PasswordTooWeakException`, `CurrencyMismatchException` — 20 types across the four contexts that have rules and
  the shared kernel.
- A request the application cannot serve is a `UseCaseException` beside the use case: `CartNotFoundException`,
  `DuplicateSkuException`, `CheckoutSessionNotFoundException`, `PaymentProviderUnavailableException` and the rest —
  16 types.
- **An argument guard stays the platform's own exception.** A null check or a range check in a value-object
  constructor states what a caller must never pass; it is not a business rule and no rule of the catalog selects
  it. The cut is the name: if a domain expert has a word for the failure, it is a domain exception with that word
  in it.
- **The incoming adapter translates, and it is the only layer that does.** `ProductApiExceptionHandler`,
  `CartApiExceptionHandler` and `AccountApiExceptionHandler` are `IExceptionHandler`s in their own context's
  `Adapter/Incoming/Api` namespace, each producing a `ProblemDetails` (RFC 9457) through `AddProblemDetails()`.
  The page controllers of Cart, Checkout and Account catch the two base types and render the message into the form
  they came from.
- **Scope is a route prefix, not a package.** Spring scopes an advice with `basePackages`; ASP.NET Core has one
  handler chain for the whole application, so each handler declines a request whose path is not its own
  (`/api/products` and `/mcp`, `/api/carts`, `/api/auth`) and the next context's handler gets its turn. The chain
  runs only for the token-only paths: `Program.cs` branches it with `app.UseWhen(TokenOnlyPaths.IsTokenOnlyEndpoint
  …)`, and the pages keep `UseExceptionHandler("/error")`. It is registered after that endpoint so it is the inner
  handler and sees an API failure first.
- **The status follows the failure, not the base type.** `CartItemNotFoundException` is a rule of the model and
  still answers `404`, because what the caller has to do about it is ask for something that exists. Deciding that
  is the adapter's job: the same use case serves the REST exposure and a page with different answers.
- **Neither base type carries a code or a status field.** That would be the adapter's decision taken in the inner
  layer.

**The result channel stays where it is.** Account and Inventory already answer some outcomes with a result variant
(`ChangePasswordResult.Outcome`, `ReduceStockResult.Failure`) rather than an exception, and that does not change.
What changed is that the catch which converts a domain rule into such a variant now names the rule's own type:
`ChangePasswordUseCase` catches `PasswordTooWeakException`, `ReduceStockUseCase` catches
`InsufficientStockException`, and a malformed call from an adapter no longer arrives in the same clause.

## Consequences

- The build needs `-p:UseLocalDcaDotnet=true` until `DomainCentric.BuildingBlocks` ships the two base types. Until
  then this work lives on `wp-22-dotnet-sample`; CI on `main` keeps building against the published versions.
- `DCA-ERR-001` … `DCA-ERR-005` hold the shape from now on: a new exception in a domain or application namespace
  must extend the base type of its layer, live there, carry no framework attribute and name no transport concept.
- `DCA-ERR-006` lists the incoming adapter namespaces that name no failure type. Nine remain — the same nine as in
  the Java sample: four event-consumer namespaces, where a failed reaction belongs to the delivery machinery's
  retry rather than to an answer; the bootstrap seeder, which runs before any customer exists; the product page and
  MCP tool namespaces, whose queries report "not found" as a result flag and raise nothing; the cart recovery page,
  which only parses a merge strategy; and the backoffice page. The last two are the ones worth revisiting when
  those flows grow a refusal of their own.
- `RegisterResponse` lost its failure factory: a refused registration is a problem document now, so the success
  body no longer carries an error field that was always null.
- **The active-cart claim had to change with it, and that is not a detail.** `InMemoryShoppingCartRepository` now
  raises `ActiveCartAlreadyExistsException` — the failure the port declares — instead of a bare invalid-operation.
  Two things had to move for the contract to hold under a race, and the Java sample paid for both first (its
  ADR-045): the adapter **stores the cart before it publishes the claim** and withdraws it when the claim fails,
  because two separate map writes are not one commit and the request that lost could otherwise read the winner's id
  out of the index with nothing behind it; and `GetOrCreateActiveCartUseCase` **claims inside its own transaction
  and recovers outside it**, because a store that refuses a write mid-transaction leaves that transaction unusable.
  `ActiveCartRaceTest` drives eight simultaneous callers through the wired store and is what proved it — every
  sequential test was green while the ordering was still wrong.
- The remaining imprecision is deliberate and marked in each handler: `ArgumentException` is still mapped to `400`,
  because a form field reaches a value object unvalidated. Every field moved into request validation is one reason
  less for that mapping to exist.
- **Harness questions.** Catalog: nothing new — the marker nodes, the six rule nodes, the `domain-exception`
  template and the pitfall "a business rule reported as an argument error" were written with the libraries
  (WP-22 §1–§3, catalog commit `58a9084`), and this sample only consumes them. Rule: nothing new —
  `DCA-ERR-001…006` already cover the shape, and the one thing they cannot see (a transport-status attribute on a
  domain exception) is the open gap ADR-002 of `dca-dotnet` records, unchanged by this work. Marker: nothing new —
  `DomainException` and `UseCaseException` are the two the samples needed.
