---
id: a-price-that-rounds-to-zero-names-the-price
epic: prices
status: draft
created: 2026-10-24T06:35:02Z
context: pricing
title: A price that rounds to zero is refused naming the price
closes: [an-operator-changes-a-price#1]
depends_on: [the-take-over-leads-only-to-pages-of-the-shop]
---

# A price that rounds to zero is refused naming the price

## Story

As an operator who looks after prices I want a price that rounds to zero cents to be refused like a price of zero, naming the price, so that I can see which value to correct.

## Acceptance criteria

### Rule: A new price is greater than zero after rounding to cents

#### a-price-that-rounds-to-zero-is-refused-naming-the-price (happy path)
- Given a client holding a staff token, and "Domain-Driven Design" costs 54.99 EUR
- When it sends `PUT /api/products/<id of Domain-Driven Design>/price` with `{"price":0.004}`
- Then the shop answers 400 with an `application/problem+json` body
- And its title is "Unacceptable value" and its `errors` names `price` with the reason
- And the product page of "Domain-Driven Design" still shows "54.99 EUR"

## Changed expectations

- Now: a price below half a cent is refused with 400, but the answer does not name the field. After: it is answered exactly like a price of zero, naming `price`.

## Assumptions

- answered: a price is rounded to cents half away from zero, so `0.005` becomes 0.01 EUR and is accepted today; that side of the limit already holds and is not a criterion here.

## Notes

- From the findings triage (WP-151), split off `the-pricing-findings-are-closed` on the user's say-so (2026-10-24): finding #1 changes what an API client sees, so it is a story, not a task.
- The finding names "both samples": the Java sample needs the same story as its own backlog item.
