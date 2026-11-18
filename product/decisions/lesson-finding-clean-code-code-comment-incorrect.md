---
id: lesson-finding-clean-code-code-comment-incorrect
kind: lesson
lesson: finding
key: clean-code/code/comment/incorrect
stories: 12
asked: 2026-11-18T11:51:32Z
---

# The same finding in 12 stories — clean-code/code/comment/incorrect

## Question
Counted, never guessed: this finding repeats in 12 stories. Where does its correction belong? The factory writes no rule and no knowledge itself — the skill that owns the place does, with the evidence below. No story waits for this answer.

## Evidence
- registration-says-the-account-was-created: registration-says-the-account-was-created#4
- the-inventory-findings-are-closed: the-inventory-findings-are-closed#3
- an-open-checkout-keeps-its-price: an-open-checkout-keeps-its-price#2
- the-pricing-findings-are-closed: the-pricing-findings-are-closed#2
- paying-again-requests-no-second-payment: paying-again-requests-no-second-payment#3
- a-date-of-birth-lies-in-the-past: a-date-of-birth-lies-in-the-past#3
- api-answers-an-unknown-product-plainly: api-answers-an-unknown-product-plainly#1
- cart-list-spells-the-status: cart-list-spells-the-status#2
- created-product-answers-with-its-price: created-product-answers-with-its-price#2
- the-cart-oversight-has-its-own-place-in-the-code: the-cart-oversight-has-its-own-place-in-the-code#2
- the-cart-oversight-totals-at-todays-prices: the-cart-oversight-totals-at-todays-prices#1
- only-the-browsers-own-guest-cart-is-taken-over: only-the-browsers-own-guest-cart-is-taken-over#2
- contrast: taking-over-the-guest-cart-is-a-step-of-its-own — the same target without this finding

## Options
- a: instructions — a line of the project's agent instructions (written by `/dca-describe`)
- b: factory — the factory itself got this wrong — an issue text for its maintainers (`factory.sh lessons --issue lesson-finding-clean-code-code-comment-incorrect`, sent by a person)
- c: not-now — asked again once the evidence has grown
- d: never — the same candidate does not come back

## Recommendation
a — instructions: no rule can see it; a line of the agent instructions tells the stages before they write it.
