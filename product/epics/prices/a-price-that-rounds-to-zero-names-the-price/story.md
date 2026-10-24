---
id: a-price-that-rounds-to-zero-names-the-price
epic: prices
status: approved
created: 2026-10-24T06:35:02Z
context: pricing
title: A price that rounds to zero is refused naming the price
closes: [an-operator-changes-a-price#1]
depends_on: [the-take-over-leads-only-to-pages-of-the-shop]
---

# A price that rounds to zero is refused naming the price

## Story

As an operator who looks after prices I want a price that rounds to zero cents, or lies above the highest amount, to be refused like a price of zero, naming the price, so that I can see which value to correct.

## Acceptance criteria

### Rule: A new price lies between 0.01 EUR and the highest amount after rounding to cents, and a refusal names the price

#### a-price-that-rounds-to-zero-is-refused-naming-the-price (happy path)
- Given a client holding a staff token, and "Domain-Driven Design" costs 54.99 EUR
- When it sends `PUT /api/products/<id of Domain-Driven Design>/price` with `{"price":0.004}`
- Then the shop answers 400 with an `application/problem+json` body
- And its title is "Unacceptable value" and its `errors` names `price` with the reason
- And the product page of "Domain-Driven Design" still shows "54.99 EUR"

#### a-price-above-the-highest-amount-is-refused-naming-the-price
- Given a client holding a staff token, and "Domain-Driven Design" costs 54.99 EUR
- When it sends `PUT /api/products/<id of Domain-Driven Design>/price` with `{"price":1000000000000}`
- Then the shop answers 400 with an `application/problem+json` body
- And its title is "Unacceptable value" and its `errors` names `price` with the reason
- And the product page of "Domain-Driven Design" still shows "54.99 EUR"

## Changed expectations

- Now: a price below half a cent, or above 999 999 999 999.99 EUR, is refused with 400, but the answer does not name the field. After: both are answered exactly like a price of zero, naming `price`.

## Assumptions

- answered: a price is rounded to cents half away from zero, so `0.005` becomes 0.01 EUR and is accepted today; that side of the limit already holds and is not a criterion here.
- answered: the highest amount, 999 999 999 999.99 EUR, is accepted today; only the refusal above it is a criterion. The user decided (2026-10-24) the upper limit belongs in this story.

## Notes

- From the findings triage (WP-151), split off `the-pricing-findings-are-closed` on the user's say-so (2026-10-24): finding #1 changes what an API client sees, so it is a story, not a task.
- The finding names "both samples": the Java sample needs the same story as its own backlog item.
