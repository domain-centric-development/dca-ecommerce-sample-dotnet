# ADR-007: API Authorization at the Adapter, and a Bearer-Only `/api` and `/mcp`

**Date**: 2026-08-30 · **Status**: Accepted

## Context

Stage 2c adds the REST resources (`/api/products`, `/api/carts`, `/api/auth`) and the MCP server (`/mcp`). Two
questions had to be answered before writing a single endpoint.

**Who may reach what?** The Java sample's API, which this one is ported from, is effectively open:
`GET /api/carts` returns every cart of every customer, `GET /api/carts/{id}` returns any stranger's cart, and
`POST /api/products` lets an anonymous caller create a product. Its `anyRequest().authenticated()` does not catch
this, because the JWT filter gives *every* request an authentication — an anonymous identity is still an
authentication. The same is true here: `JwtAuthenticationMiddleware` enriches, it does not gate (ADR-006).
Copying that shape would ship an open API as a reference implementation.

**What authenticates an API call?** ADR-005 exempts a token-only API from antiforgery, but only on the condition
that cookies never authenticate it. Until now no such endpoint existed, so the condition had never been met by
anything — the middleware read a Bearer header as a *fallback* for the session cookie, on every path.

## Decision

**A guard goes where its inputs are.** The discriminator is not "business or technical" — it is whether the
check needs the aggregate.

- **Claims only, no resource** → the adapter. `POST /api/products` and `GET /api/carts` (every cart in the shop)
  require the staff role, read through `IIdentityProvider.IIdentity.HasRole(RoleStaff)`. This is a property of
  the *exposure*, not of the operation: `GetAllCartsUseCase` is a legitimate thing for an admin console or a
  batch job with no HTTP identity to run, and forcing an identity port into it would make it unusable there.
  No registration path hands the staff role out; an account only gets it by being given it.
- **Ownership of a resource** → the use case, always. It can never be exposure-shaped: *no* caller may act on a
  stranger's cart, through *any* adapter. So the caller is part of the command — `GetCartByIdQuery(CartId,
  CustomerId)`, `CheckoutCartCommand(CartId, CustomerId)` — and the use case reads through
  `IShoppingCartRepository.FindByIdForCustomerAsync`, which cannot return somebody else's cart. Cart's Open Host
  Service demands the customer for the same reason, so the Checkout context inherits the rule instead of
  repeating it.
- **The catalog is public to read** (`GET /api/products`, `GET /api/products/{id}`) — the same assortment the shop
  pages show anonymously.

**The decision belongs to the use case; its rendering belongs to the adapter.** A cart that is not the caller's
answers `404`, not `403` — a `403` would confirm that the id exists, which is exactly the fact a stranger must not
learn. That choice is an HTTP concern, so the resource makes it; what it renders is the use case's answer.

**A use case with no caller is not under-specified.** `CompleteCart` runs from an integration event, at least
once, on nobody's behalf — there is no identity to check and it stays unscoped.

**`/api/**` and `/mcp` are authenticated by an `Authorization: Bearer` header and by nothing else.**

- `JwtAuthenticationMiddleware.TokenOnlyPathPrefixes` names those paths. On them the middleware reads only the
  Bearer header: it does not read a cookie, and it does not write one. A request from a browser that carries both
  shop cookies arrives at the API as a stranger.
- `TokenOnlyAwareAntiforgeryFilter` replaces the global `AutoValidateAntiforgeryTokenAttribute`. It validates
  every unsafe method except on those same paths, and it asks the middleware which paths those are rather than
  keeping a second list that could drift.
- `AuthResource` returns the token in the response body and sets no cookie. A browser session is established by
  the login *form* instead, which does get cookies and does need the token.

The two halves are one decision: the antiforgery exemption is sound **only** because no cookie authenticates
those paths. Changing one without the other is the mistake to watch for in review.

## Consequences

- Positive: no endpoint exposes another customer's cart, and no anonymous caller can create a product. The same
  correction was applied to the Java sample, so the two do not contradict each other.
- Positive: the exemption is testable, and tested. `ApiFlowTest` asserts both halves — a browser cookie neither
  authenticates an API call nor is handed out by one, and an API `POST` without an antiforgery token still works
  while a web `POST` without one is still refused.
- Positive: the ownership rule reaches adapters this ADR is not about. `POST /checkout/start` took its `cartId`
  from a hidden form field and `StartCheckoutUseCase` never looked at the caller, so a visitor who learned a cart
  id could open a checkout session on somebody else's cart in their name. Putting the caller into
  `StartCheckoutCommand` closed it; no amount of discipline in the REST resource would have reached it.
  `CartOwnershipTest` covers it.
- Negative: the coarse role gate in the adapter is not enforced by the framework. A new route that forgets it
  compiles and runs. The integration tests are the guard; a policy-based `[Authorize]` scheme over a real claims
  principal would move it into the pipeline, and is what a production system should do.
- Negative: `FindByIdForCustomerAsync` sits next to `FindByIdAsync`, and a use case that reaches for the wrong one
  is back where it started. The scoped one is the default and the unscoped one is documented as the system path,
  but nothing enforces the choice.
- Negative: the staff role has no provisioning path. Granting it means minting a token out of band, which is what
  the tests do. That is honest for a sample and would not be acceptable in a real shop.
- Neutral: `POST /api/carts` takes no `customerId`, and `DELETE /api/carts/{cartId}/items/{itemId}` names the
  cart item rather than the product — both follow from the decision above and from the shape of the use cases,
  and both differ from the Java routes.
- Neutral: six of Cart's use cases already took the caller as input (`CreateCart`, `GetOrCreateActiveCart`,
  `GetActiveCart`, `MergeCarts`, `RecoverCartOnLogin`, `GetCartMergeOptions`). The five that did not are exactly
  the five that needed a guard bolted onto the adapter. Read that way this is less a decision about authorization
  than a correction of five under-specified commands: "check out cart X" without saying on whose behalf is an
  incomplete instruction.
