---
id: price-rounding-to-zero-is-unacceptable
epic: shop-api
status: delivered
delivered: 2026-10-13T17:14:37Z
delivered_by: gate
created: 2026-10-13T19:17:38Z
context: product
feature: assortment
title: A price that rounds to zero is an unacceptable value
depends_on: [created-product-answers-with-its-price]
---

# A price that rounds to zero is an unacceptable value

## Story

As a staff member who creates a product I want a price below a cent refused the way every unusable value is,
so that my client reads one kind of refusal for every bad value.

## Acceptance criteria

### Rule: A price is greater than zero

#### a-price-that-rounds-to-zero-is-refused (happy path)
- Given a client holding a staff token
- When it creates "Tiny Mug" with SKU "TINY-001", price 0.001, category "Desk & Office" and stock 5
- Then the shop answers 400 with an `application/problem+json` body
- And its title is "Unacceptable value" and it carries no detail

## Changed expectations

- A price below a cent is refused 400 by the request's validation now — a validation problem with its own title
  — and 400 "Unacceptable value" without a detail after, the price rounded to zero and refused by the domain.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `creating-a-product-holds-its-price-01`).
