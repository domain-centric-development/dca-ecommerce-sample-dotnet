---
id: reading-the-active-cart-opens-none
epic: shop-api
status: approved
context: cart
feature: shopping
title: Reading a customer's active cart opens none
depends_on: []
---

# Reading a customer's active cart opens none

## Story

As a partner application I want reading a customer's active cart to answer that there is none when there is none,
so that a read never leaves a cart behind and opening a cart stays the one way to get one.

## Acceptance criteria

### Rule: Reading the active cart opens none; a customer without one is answered 404

#### no-active-cart-is-not-found (happy path)
- Given a client holding the token of the customer "grace@example.com", who has no cart yet, and her `customerId`
- When it sends `GET /api/carts/customer/<her customerId>/active` with her token
- Then the shop answers 404

#### a-read-leaves-no-cart-behind
- Given the client of "grace@example.com" has just been answered 404 for her active cart
- When it sends `POST /api/carts` with her token
- Then the shop answers 201
- And the body has `items` [], `status` "ACTIVE" and `itemCount` 0

## Changed expectations

- Reading the active cart of a customer who has none opens an empty cart and answers 200 with it now; it answers
  404 and opens nothing after, and opening a cart afterwards answers 201 instead of 409.

## Notes

- Decided by the user 2026-10-14 (WP-108 part A): a read never creates a cart. A cart comes into being when it is
  opened (`POST /api/carts`) or when an item is put in it. One of the two shops already behaves so and adopts this
  story; the other builds it.

## Assumptions

- answered: SharedScenariosTest also knows the titles of the stories not yet delivered (their `####` criteria, title = Title line else the key in words); the spec stays the only source for the Test-line and duplicate checks. Fixed on main in 2ea4aaa; nothing changes in this story — run the stage again as it is. (reading-the-active-cart-opens-none-01)
- answered: Bring the sibling story's delivered scenario into `product/spec` on main (the merge its delivering gate should have (reading-the-active-cart-opens-none-02)
- answered: a — the delivered stories' scenarios are committed on main now (6a8d286); merge main into the branch and run the build again as it is. (reading-the-active-cart-opens-none-03)
