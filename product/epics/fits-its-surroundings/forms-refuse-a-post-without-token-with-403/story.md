---
id: forms-refuse-a-post-without-token-with-403
epic: fits-its-surroundings
status: delivered
delivered: 2026-10-13T07:38:37Z
delivered_by: gate
context: account
title: A form post without its token is refused with 403
depends_on: [register-an-account, log-in-and-log-out]
---

# A form post without its token is refused with 403

## Story

As the shop's owner I want a form post that did not come from the shop's own pages refused as forbidden, so that
a client reads the refusal for what it is.

## Acceptance criteria

### Rule: A form post without the token of the shop's page is refused with 403

#### a-login-post-without-the-form-token-is-refused (happy path)
- Given an account "ada@example.com" with password "Secret123"
- When a browser posts email "ada@example.com" and password "Secret123" to `/login` without the form token the login page carries
- Then the shop answers 403
- And the visitor is not logged in: the header still shows "Login" and "Register"

#### a-registration-post-without-the-form-token-is-refused
- Given no account exists for "grace@example.com"
- When a browser posts a complete, valid registration for "grace@example.com" to `/register` without the form token
- Then the shop answers 403
- And logging in as "grace@example.com" afterwards fails with "Invalid email or password"

#### a-logout-post-without-the-form-token-is-refused
- Given a customer logged in as "ada@example.com"
- When a post to `/logout` arrives from her browser without the form token
- Then the shop answers 403
- And the header still shows "Welcome, ada@example.com"

## Changed expectations

- A form post without the form token is answered 400 now and 403 after; nothing it asked for is done, before and after.
- `AccountFlowTest.LoggingInWithoutAnAntiforgeryTokenIsRejected` and `ShopFlowTest.PostWithoutAntiforgeryTokenIsRejected`
  expect 403.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `the-shops-forms-refuse-posts-that-did-01`).
