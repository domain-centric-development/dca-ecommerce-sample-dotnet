---
id: cart-list-spells-the-status
epic: shop-api
status: delivered
delivered: 2026-10-13T12:10:31Z
delivered_by: gate
context: cart
feature: shopping
title: The staff's cart list spells a cart's status as every answer does
depends_on: [cart-api-answers-as-the-reference]
---

# The staff's cart list spells a cart's status as every answer does

## Story

As a staff member who lists every cart through the API I want a cart's status spelled as the single cart's answer
spells it, so that one client reads both the same way.

## Acceptance criteria

### Rule: Only staff list every cart; a stranger is challenged, a customer is forbidden

#### staff-list-every-cart (happy path)
- Given Ada's cart holds "Domain-Driven Design" × 2 and a client holding a staff token
- When it sends `GET /api/carts`
- Then the shop answers 200 with `carts`, one of which has Ada's `cartId`, her `customerId`, `status` "ACTIVE", `itemCount` 1, `total` 109.98 and `currency` "EUR"

## Changed expectations

- A listed cart's `status` reads "Active" now and "ACTIVE" after.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `only-your-own-cart-through-the-api-01`).
