---
id: cart-api-takes-any-product-id
epic: shop-api
status: delivered
delivered: 2026-10-13T16:43:03Z
delivered_by: gate
created: 2026-10-13T19:17:38Z
context: cart
feature: shopping
title: The cart API answers any product id
depends_on: [cart-api-answers-as-the-reference]
---

# The cart API answers any product id

## Story

As a partner application I want the cart API to answer a product id it does not know as an unavailable article,
whatever the id looks like, so that I handle one answer for every unknown product.

## Acceptance criteria

### Rule: A product the catalogue does not know cannot be added

#### an-unknown-product-is-not-available (happy path)
- Given Ada's open, empty cart
- When her client sends `POST /api/carts/<cartId>/items` with `{"productId":"no-such-product","quantity":1}`
- Then the shop answers 404 with an `application/problem+json` body
- And its title is "Article not available" and its detail "There is no article for product no-such-product."

## Changed expectations

- Adding a product with the id "no-such-product" is refused 400 by the request's binding now and answered 404
  "Article not available" after.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `the-cart-api-refuses-what-the-cart-01`).
