---
id: created-product-answers-with-its-price
epic: shop-api
status: delivered
delivered: 2026-10-13T07:49:01Z
delivered_by: gate
context: product
feature: assortment
title: A created product answers with its price, and a page without description says so
depends_on: [CAT-02, register-through-the-api]
---

# A created product answers with its price, and a page without description says so

## Story

As a staff member who creates a product I want the answer to carry the price I gave, and a visitor on a product
without description to read that there is none, so that neither of us is left guessing.

## Acceptance criteria

### Rule: Staff create a product; its price is always in euros

#### staff-create-a-product (happy path)
- Given a client holding a token whose roles include staff
- When it sends `POST /api/products` with `{"sku":"MUG-002","name":"Aggregate Mug","description":"A mug with a consistency boundary.","imageUrl":"","price":14.99,"category":"Desk & Office","stock":12}`
- Then the shop answers 201
- And the body has an `id`, `sku` "MUG-002", `name` "Aggregate Mug", `price` 14.99, `currency` "EUR" and `category` "Desk & Office"

### Rule: A product without a description says so on its page

#### a-product-page-without-description
- Given staff created "Bounded Context Tote" without a description
- When a visitor opens its product page
- Then the description reads "No description available."

## Changed expectations

- The 201 answer to `POST /api/products` carries `price` and `currency` as `null` now and as given after.
- A product page without a description shows no description block now and "No description available." after.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `staff-create-a-product-through-the-api-01`).
