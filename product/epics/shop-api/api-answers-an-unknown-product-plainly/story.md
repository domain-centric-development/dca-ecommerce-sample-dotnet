---
id: api-answers-an-unknown-product-plainly
epic: shop-api
status: delivered
delivered: 2026-10-12T21:59:58Z
delivered_by: gate
created: 2026-10-12T21:08:53Z
context: product
feature: catalogue
title: The API answers an unknown product with a plain 404
depends_on: [CAT-01]
---

# The API answers an unknown product with a plain 404

## Story

As a partner application I want the API to answer a product address that names no product with a plain 404, so
that I am not handed a web page where I read JSON.

## Acceptance criteria

### Rule: An unknown product is not found

#### an-unknown-product-is-not-found (happy path)
- Given the seeded sample catalogue
- When a client sends `GET /api/products/no-such-product`
- Then the shop answers 404 with an empty body

## Changed expectations

- `GET /api/products/no-such-product` answers 404 with the HTML not-found page now, and 404 with an empty body after.

## Out of scope

- The catalogue's list and a known product through the API — `read-the-catalogue-through-the-api`, which adopts them.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `read-the-catalogue-through-the-api-01`).
