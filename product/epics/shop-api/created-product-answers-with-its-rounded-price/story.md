---
id: created-product-answers-with-its-rounded-price
epic: shop-api
status: adopted
context: product
feature: assortment
title: A created product answers with its price rounded to cents
depends_on: [staff-create-a-product-through-the-api]
---

# A created product answers with its price rounded to cents

## Story

As a member of staff who creates a product I want the answer to carry the price the shop holds — two decimals,
rounded half up — so that no fraction of a cent reaches anyone, the answer included.

## Acceptance criteria

### Rule: A price is rounded to two decimals, half up

#### the-answer-carries-the-rounded-price (happy path)
- Given a client holding a staff token
- When it creates "Rounding Mug" with SKU "ROUND-001", price 14.995, category "Desk & Office" and stock 5
- Then the shop answers 201 with `price` 15.00

## Notes

- Adopted 2026-10-14 (WP-108): this shop already answers with the rounded price; the Java twin delivered the story as
  a change, so both specs hold the criterion.
- A price is a decimal with two places and rounds by the commercial rule (half up), as `Money` already does.
