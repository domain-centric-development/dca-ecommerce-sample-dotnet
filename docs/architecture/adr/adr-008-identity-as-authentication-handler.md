# ADR-008: The Shop's Identity as an ASP.NET Core Authentication Handler

**Date**: 2026-09-03 · **Status**: Accepted · Amends [ADR-006](adr-006-identity-and-session-cookies.md) point 1 and
[ADR-007](adr-007-api-authorization-and-bearer-only-boundary.md)

## Context

ADR-006 chose to resolve the two shop cookies in an own middleware that parsed the tokens by hand and put the
resulting `IIdentityProvider.IIdentity` on `HttpContext.Items`. Controllers then guarded by hand:
`if (!identity.HasRole(RoleStaff)) return StatusCode(403)` in the API resources, `if (identity.IsAnonymous) return
Redirect(login)` in the account pages. It worked, and it was documented — but it sat beside ASP.NET Core's
authentication model rather than inside it, and ADR-007 already named the price: *"the coarse role gate in the
adapter is not enforced by the framework. A new route that forgets it compiles and runs … a policy-based
`[Authorize]` scheme over a real claims principal would move it into the pipeline, and is what a production system
should do."* An external review of the sample made the same point.

What the hand-rolled path forwent:

- `HttpContext.User` stayed empty, so nothing in the framework — `[Authorize]`, authorization policies, logging
  scopes, the antiforgery system's identity binding, diagnostics — knew who was calling.
- There were no schemes and therefore no challenge: a stranger and a customer without the staff role both got
  `403`, although only the second is *forbidden*; the first was never *authenticated*, which is `401` and a
  `WWW-Authenticate` header telling them how.
- The Java twin *is* framework-native (`OncePerRequestFilter` populating the `SecurityContext`), so the two samples
  had drifted in exactly the place where ADR-006 wanted them readable side by side.

The design itself — two cookies, two lifetimes, identity resolved before and independently of the session, an
authentication that enriches and never gates, a Bearer-only `/api` and `/mcp` — is not in question. Only its
seat in the pipeline is.

## Decision

**The identity is an authentication scheme; the framework reads `HttpContext.User`, the port reads the identity the scheme recorded.**

1. **One handler, two schemes.** `ShopIdentityAuthenticationHandler : AuthenticationHandler<…>` carries the
   resolution logic of the former middleware unchanged and is registered twice: `ShopCookies` reads the two
   cookies of ADR-006 and never the `Authorization` header; `ShopBearer` reads the header and never a cookie.
   The default scheme, `Shop`, is a *policy scheme* whose `ForwardDefaultSelector` picks one of the two by request
   path, asking `TokenOnlyPaths` — the same list the antiforgery filter asks (ADR-007). No controller names a
   scheme; `[Authorize]` on a page and `[Authorize]` on a resource behave differently because the path does.

2. **The principal is the framework's currency, the port is the application's.** For a registered session
   `ShopPrincipal` maps the `IIdentity` to a `ClaimsPrincipal` (`NameIdentifier` = `UserId`, `Email`, `Role`, plus
   an identity-type claim) that becomes `HttpContext.User`. The port is served separately: the handler records the
   resolved identity — anonymous or registered — as an `IShopIdentityFeature` on the `HttpContext`, and
   `HttpContextIdentityProvider` reads that feature. Every consumer of `IIdentityProvider` — page controllers,
   resources, the mini basket — is untouched, and the port is still the only thing the application layer sees.
   The feature, not the principal, is the port's source because `HttpContext.User` is not stable across a request:
   an `[Authorize]` naming another scheme (the backoffice cookie) replaces it with that scheme's principal, and the
   header's mini basket on a backoffice page still needs the visitor's `UserId`. That is the same mechanism the
   framework uses for its own result (`IAuthenticateResultFeature`) — it is not `HttpContext.Items` in disguise,
   because the framework now sees the identity where it looks for one.

3. **Anonymous is an identity, not an authentication.** Every request ends with an identity on it — the anonymous
   visitor's `UserId` keys the cart — but only a registered session produces an *authenticated* principal. For an
   anonymous visitor the handler reports `NoResult`, because the policy evaluator treats a *succeeded* scheme as
   authenticated regardless of its identity: a `Success` ticket with an unauthenticated principal would make
   `[Authorize]` *forbid* an anonymous visitor instead of *challenging* them. With `NoResult` the pipeline does
   what the two words mean: a stranger is challenged, a customer without the role is forbidden.

4. **Challenges are scheme-shaped.** The cookie scheme redirects to `/login?returnUrl=…` — the same redirect the
   account pages issued by hand. The Bearer scheme answers `401` with `WWW-Authenticate: Bearer` and an RFC 9457
   problem document; a registered caller without the required role gets `403` the same way. Writing a body also
   keeps the status-code pages middleware from re-executing an API request onto the HTML error page.

5. **Guards move into attributes where they were claims-only.** `POST /api/products` and `GET /api/carts` carry
   `[Authorize(Roles = RoleStaff)]` and `ProductResource` no longer injects the identity port at all. The account
   pages carry `[Authorize]`. Nothing else moves: an **ownership** check stays in the use case, as a command field
   and a scoped repository question (ADR-007 is unchanged there).

6. **The backoffice scheme is no longer the default.** It keeps its own cookie and its own credentials, is
   registered with `AddAuthentication()` without a default, and every backoffice page names it in `[Authorize]`, as
   it already did.

7. **Cookie writing stays in the Account adapter.** `IIdentitySession` (`SetRegisteredIdentity`, `LogOut`) still
   writes and rotates the cookies through `CookieWriter`; the handler does not implement `SignInAsync` /
   `SignOutAsync`. Doing so would mean expressing the identity-rotation rules of ADR-006 as `AuthenticationProperties`
   for no gain — the port's shape is the Java sample's and is an open question for both (root `TODO.md` #24).

### Not chosen: the built-in `JwtBearer` handler for `/api`

Microsoft's handler validates the same HS256 token, but it yields **no principal at all** when there is no token,
where this design needs an anonymous identity with a fresh `UserId`; it would need an `OnTokenValidated` hook for
the deleted-account check; and it would split token reading across two code paths with two claim mappings.
`JwtTokenService` stays the single place that reads a token and distinguishes *expired* from *unreadable*.

## Consequences

- Positive: `HttpContext.User` carries every registered session; `[Authorize]`, policies and the challenge pipeline
  work; a forgotten guard is now a missing attribute, which review catches, not a missing `if` in a method body.
- Positive: `401` and `403` mean what they say on the API. `ApiFlowTest` asserts a stranger gets `401` with
  `WWW-Authenticate: Bearer` and a customer without the role gets `403`.
- Positive: the two samples are framework-native on both sides again. The Java sample still answers `403` to an
  anonymous API caller — a follow-up to align it (root `TODO.md`).
- Negative: two request-scoped views of the same identity — the principal for the framework, the feature for the
  port — built from one object in one place, but a reader has to be told why both exist (point 2). It is commented
  at the spot.
- Verified: 127 unit, 29 integration and 113 architecture tests green; the sample's Playwright suite 16/16 and the
  Java sample's Playwright suite 16/16 against this shop, both on 2026-09-03.
- Negative: the Bearer scheme returns a fresh anonymous `UserId` per request when no token is present, exactly as the
  middleware did; nothing on `/api` can be keyed on it. Unchanged behaviour, now stated.
- Neutral: `JwtAuthenticationMiddleware` is deleted; `TokenOnlyPaths`, `ShopPrincipal` and `IShopIdentityFeature`
  are new; the account pages lost their hand-written anonymous checks and their `LoginRedirect()` helpers now cover
  only the account that no longer exists.

## Related

- [ADR-006](adr-006-identity-and-session-cookies.md) — the two-cookie design this handler implements; its point 1
  ("JWT, not ASP.NET Core cookie authentication") is amended: still JWT, now as an authentication scheme.
- [ADR-007](adr-007-api-authorization-and-bearer-only-boundary.md) — the Bearer-only boundary; the path list now
  lives in `TokenOnlyPaths`, and the claims-only gates it placed in the adapter are attributes.
- [ADR-005](adr-005-antiforgery-and-safe-methods.md) — the antiforgery filter unchanged, asking the same path list.
