# ADR-005: Antiforgery Token on Every Writing Form, No State-Changing GET

**Date**: 2026-08-30 · **Status**: Accepted

## Context

The shop identifies the browser with a cookie (`dcashop-customer`) and every writing controller action resolves
the customer from it. A cross-site page can make the browser send that cookie. Two things made that exploitable in
the first stage: writing actions accepted requests without an antiforgery token, and the checkout was started by a
`GET` link (`/checkout/start?cartId=…`) — a state change reachable by prefetching, crawlers and any cross-site
navigation, which no token can protect because `GET` carries none.

The Java sample made the same two changes at the same time (its ADR-035); the two samples render the same markup.

## Decision

- `AutoValidateAntiforgeryTokenAttribute` is a global MVC filter: every unsafe method (`POST`, `PUT`, `DELETE`,
  `PATCH`) is rejected with `400` unless the request carries a valid antiforgery token. Every writing Razor form
  renders `@Html.AntiForgeryToken()`.
- A use case that changes state is reached only through an unsafe method. Starting the checkout is a `POST`
  form on the cart page (`cart-checkout-form`, hidden `cartId`); `GET /checkout/start` no longer exists (`405`).
- Stage 1 has no token-authenticated API; if one is added, it is exempt from antiforgery only if it neither reads
  nor issues cookies (Bearer only), mirroring the Java sample.

## Consequences

- Positive: identical protection and identical markup in both samples — the Java Playwright suite passes against
  this shop unchanged; the only visible difference is the token field name (`__RequestVerificationToken` vs
  `_csrf`).
- Positive: the integration test `ShopFlowTest` asserts the negative case (POST without token → `400`) next to the
  happy path, so the filter cannot be removed unnoticed.
- Negative: every form needs the helper; forgetting it surfaces as a `400` in the first manual click, not at
  compile time.
