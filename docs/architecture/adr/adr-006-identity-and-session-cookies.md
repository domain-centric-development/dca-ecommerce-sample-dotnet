# ADR-006: Two Cookies for Identity and Session, Signed by an Own JWT Middleware

**Date**: 2026-08-30 · **Status**: Accepted

## Context

Until now the shop identified a browser with one cookie, `dcashop-customer`, minted by a helper in the Cart
context (`GuestCustomer`) and read again in Checkout and in the header. It carried a guest id and nothing else,
which was enough while nobody could log in. With the Account context there are two questions to answer that one
cookie cannot answer at once:

- **Who is this browser?** The cart, the checkout session and every other context key their data on that answer.
  It has to survive a browser restart, and it must survive an expired login — nobody logged out, so nobody should
  lose their cart.
- **Is this browser authenticated?** That answer is worth much less and should live much shorter: it is what an
  attacker gains by stealing a token.

Putting both in one cookie couples the two lifetimes: either the login lives as long as the identity, or the
identity dies as early as the login. The Java sample answered this in its ADR-029 (*session expiry ends the
session, not the identity*) and ADR-030 (*separate cookies for identity, session and renewal*), and the
implementation guide prescribes that cookie design. The two samples are the same shop, so this one adopts it.

## Decision

**Two cookies, two lifetimes, one middleware.**

| Cookie | Carries | Lifetime | Ends when |
|---|---|---|---|
| `shop-identity` | the visitor's `UserId` — the key the cart is stored under | 30 days | explicit logout only, and then it is *rotated*, not deleted |
| `shop-session` | the authentication: subject, email, roles | 7 days | expiry — with no effect on the identity |

1. **JWT, not ASP.NET Core cookie authentication.** Both cookies carry an HS256-signed token minted by
   `JwtTokenService`, and `JwtAuthenticationMiddleware` resolves them into an
   `IIdentityProvider.IIdentity` on `HttpContext.Items`. `AddAuthentication().AddCookie()` would be the idiomatic
   .NET choice for a single application, but it hides exactly the design this sample exists to show — and the Java
   twin has to be readable next to it. The token design is the guide's, not this sample's invention.

2. **The middleware enriches, it does not gate.** A request with an expired or forged session is not an error: it
   continues as anonymous and sees what an anonymous visitor sees. Authorization for a protected page is enforced
   by that page (`/account*` redirects to the login form), never by the middleware.

3. **The identity is resolved first and independently of the session.** A readable `shop-identity` decides who the
   browser is. Only if there is none does the middleware adopt a valid session's `UserId`, and only if there is
   neither does it mint one. A token in the identity cookie is used for its subject alone — its authentication
   claims, if any, are ignored.

4. **Expired and unreadable are different outcomes.** `JwtTokenService.Validate` returns `Valid`, `Expired` or
   `Unreadable`: the first two are routine, the third is an attack or a bug and is logged as a warning. Collapsing
   both failures into "no identity" would erase that distinction at the boundary.

5. **Cookie hardening is not optional.** `HttpOnly` always, `SameSite=Lax` explicit, `IsEssential` (the cart
   depends on the identity cookie, so it is strictly necessary and no consent decision may suppress it), and
   `Secure` **from configuration** — so local HTTP development cannot bake `false` into a deployment. One place
   writes an auth cookie (`CookieWriter`).

6. **Logout rotates the identity and clears the session.** It deletes no cart: the account's cart is keyed on the
   account and comes back at the next login, while the next person on a shared device inherits nothing.

7. **`GuestCustomer` is gone.** Cart, Checkout and the header mini basket read `IIdentityProvider` — the port
   lives in the shared kernel, because every context keys its data on the identity, and its only implementation
   lives in Account, because only Account can establish one.

### Deliberately deferred: `shop-refresh`

The Java ADR-030 describes a third, path-scoped renewal cookie backed by a server-side row. It is not implemented
here either, and the price is worth naming: **without a refresh token there is no revocation and no theft
detection, so the session cookie's lifetime is the blast radius of a stolen token.** A renewal flow needs a
persistent token store, rotation with reuse detection and an endpoint to scope the cookie to — a subsystem larger
than the rest of this ADR. Both samples stop at the same place, on purpose.

## Consequences

- Positive: an expired session no longer costs the visitor their cart, and a logout no longer leaves the next
  person on a shared device holding it. Both are asserted in `AccountFlowTest`.
- Positive: the Java Playwright suites — including `CheckoutLoginE2ETest` and `CartMergeE2ETest` — pass against
  this shop unchanged, because the cookie names, routes and markup are the same.
- Negative: an own middleware means an own token path to maintain, outside the ASP.NET Core authentication
  handlers. It is deliberate (point 1), and it stays small: mint, validate, put the identity on the request.
- Negative: no revocation until `shop-refresh` exists — a stolen session token is valid until it expires.
- Open: whether `IIdentityProvider`, `IIdentitySession` and `ITokenService` are output ports at all is a question
  for both samples (root `TODO.md` #24); the shapes here are the Java sample's, deliberately unchanged. The same
  holds for the shared kernel declaring a port that only Account can implement — a dependency the context map does
  not show (`TODO.md` #13).

## Related

- [ADR-005](adr-005-antiforgery-and-safe-methods.md) — the login, register and logout forms are writing forms and
  carry an antiforgery token like every other.
- [ADR-001](adr-001-solution-layout.md) — Account and Portal are projects like every other context.
