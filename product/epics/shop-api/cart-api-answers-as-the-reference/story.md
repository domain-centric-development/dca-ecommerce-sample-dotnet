---
id: cart-api-answers-as-the-reference
epic: shop-api
status: approved
context: cart
feature: shopping
title: The cart API refuses a second open cart, spells its status and removes by product
depends_on: [add-a-product-to-the-cart, register-through-the-api]
---

# The cart API refuses a second open cart, spells its status and removes by product

## Story

As a partner application I want the cart API to refuse a second open cart, to spell a cart's status as the
other answers do, and to remove a product by the product's id, so that I can drive a cart without bookkeeping of my own.

## Acceptance criteria

### Rule: A cart is opened for the caller, who is always its customer

#### opening-a-cart-for-the-caller (happy path)
- Given a client holding the token of the customer "ada@example.com", who has no cart yet
- When it sends `POST /api/carts`
- Then the shop answers 201
- And the body has a `cartId`, a `customerId`, `items` [], `status` "ACTIVE" and `itemCount` 0

#### a-second-open-cart-is-a-conflict
- Given the customer "ada@example.com" already opened a cart through the API
- When the client sends `POST /api/carts` with her token again
- Then the shop answers 409 with an `application/problem+json` body
- And its title is "Active cart already exists"

#### the-callers-active-cart-by-customer-id
- Given the customer "ada@example.com", whose `customerId` the shop returned when she opened her cart
- When the client sends `GET /api/carts/customer/<her customerId>/active` with her token
- Then the shop answers 200 with that cart, same `cartId`, `status` "ACTIVE"

### Rule: Removing a product takes its whole line out

#### removing-a-product-removes-its-line
- Given Ada's cart holds "Domain-Driven Design" × 3 and "Team Topologies" × 1
- When the client sends `DELETE /api/carts/<cartId>/items/<id of Domain-Driven Design>`
- Then the shop answers 200 with the cart
- And it has one item, "Team Topologies" with `quantity` 1, and `itemCount` 1

## Changed expectations

- A second `POST /api/carts` returns the open cart with 201 now and answers 409 "Active cart already exists" after.
- A cart's `status` reads "Active" now and "ACTIVE" after.
- `DELETE /api/carts/<cartId>/items/<id>` takes a cart item's id now and the product's id after.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `open-and-fill-a-cart-through-the-api-01`).
