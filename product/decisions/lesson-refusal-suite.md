---
id: lesson-refusal-suite
kind: lesson
lesson: refusal
key: suite
stories: 17
asked: 2026-11-18T11:51:32Z
---

# The same gate refusal in 17 stories — suite

## Question
Counted, never guessed: this refusal repeats in 17 stories. Where does its correction belong? The factory writes no rule and no knowledge itself — the skill that owns the place does, with the evidence below. No story waits for this answer.

## Evidence
- a-date-of-birth-lies-in-the-past: a-date-of-birth-lies-in-the-past journal: tidy gate refused suite
- a-malformed-product-id-is-a-bad-request: a-malformed-product-id-is-a-bad-request journal: build gate refused suite
- a-place-order-with-the-review-open-asks-for-it: a-place-order-with-the-review-open-asks-for-it journal: build gate refused suite
- cart-list-spells-the-status: cart-list-spells-the-status journal: integrate gate refused suite
- choose-how-to-pay: choose-how-to-pay journal: integrate gate refused suite
- created-product-answers-with-its-rounded-price: created-product-answers-with-its-rounded-price journal: integrate gate refused suite
- each-listener-counts-its-own-attempts: each-listener-counts-its-own-attempts journal: integrate gate refused suite
- event-log-has-an-entry-per-listener: event-log-has-an-entry-per-listener journal: build gate refused suite
- reading-the-active-cart-opens-none: reading-the-active-cart-opens-none journal: build gate refused suite
- registration-says-the-account-was-created: registration-says-the-account-was-created journal: build gate refused suite
- remove-a-line-from-the-cart: remove-a-line-from-the-cart journal: build gate refused suite
- stock-checks-answer-on-the-page: stock-checks-answer-on-the-page journal: build gate refused suite
- taking-over-the-guest-cart-is-a-step-of-its-own: taking-over-the-guest-cart-is-a-step-of-its-own journal: build gate refused suite
- the-buyer-gets-an-order-confirmation: the-buyer-gets-an-order-confirmation journal: build gate refused suite
- the-cart-stays-with-the-guest: the-cart-stays-with-the-guest journal: integrate gate refused suite
- the-payment-is-authorized-at-place-order: the-payment-is-authorized-at-place-order journal: build gate refused suite
- the-shop-sets-the-shipping-price: the-shop-sets-the-shipping-price journal: integrate gate refused suite

## Options
- a: factory — the factory itself got this wrong — an issue text for its maintainers (`factory.sh lessons --issue lesson-refusal-suite`, sent by a person)
- b: profile-check — a check the profile names, run at the gate (written by `/factory-setup`)
- c: instructions — a line of the project's agent instructions (written by `/dca-describe`)
- d: not-now — asked again once the evidence has grown
- e: never — the same candidate does not come back

## Recommendation
a — factory: the gate refuses the same check story after story; either the stages are not told what it holds, or the check refuses wrongly — the factory's maintainers look at both.
