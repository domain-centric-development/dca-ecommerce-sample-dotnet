---
id: lesson-finding-clean-code-test-duplication-extraneous
kind: lesson
lesson: finding
key: clean-code/test/duplication/extraneous
stories: 18
asked: 2026-11-18T11:51:32Z
---

# The same finding in 18 stories — clean-code/test/duplication/extraneous

## Question
Counted, never guessed: this finding repeats in 18 stories. Where does its correction belong? The factory writes no rule and no knowledge itself — the skill that owns the place does, with the evidence below. No story waits for this answer.

## Evidence
- remove-a-line-from-the-cart: remove-a-line-from-the-cart#5
- the-cart-page: the-cart-page#3
- forms-refuse-a-post-without-token-with-403: forms-refuse-a-post-without-token-with-403#2
- reduced-motion-stills-the-transitions: reduced-motion-stills-the-transitions#2
- starting-and-placing-check-the-cart: starting-and-placing-check-the-cart#1
- stock-checks-answer-on-the-page: stock-checks-answer-on-the-page#2
- two-orders-never-sell-more-than-the-stock: two-orders-never-sell-more-than-the-stock#4
- the-payment-is-authorized-at-place-order: the-payment-is-authorized-at-place-order#9
- the-back-office-header-is-the-operators: the-back-office-header-is-the-operators#1
- a-malformed-product-id-is-a-bad-request: a-malformed-product-id-is-a-bad-request#2
- a-refused-api-login-is-a-problem-document: a-refused-api-login-is-a-problem-document#1
- an-unusable-cart-request-is-a-problem-document: an-unusable-cart-request-is-a-problem-document#1
- an-unusable-product-request-is-a-problem-document: an-unusable-product-request-is-a-problem-document#4
- cart-list-spells-the-status: cart-list-spells-the-status#1
- reading-the-active-cart-opens-none: reading-the-active-cart-opens-none#1
- logging-in-from-the-checkout-returns-to-it: logging-in-from-the-checkout-returns-to-it#4
- only-the-browsers-own-guest-cart-is-taken-over: only-the-browsers-own-guest-cart-is-taken-over#3
- taking-over-the-guest-cart-is-a-step-of-its-own: taking-over-the-guest-cart-is-a-step-of-its-own#5
- contrast: logging-in-brings-the-guest-cart-along — the same target without this finding

## Options
- a: instructions — a line of the project's agent instructions (written by `/dca-describe`)
- b: factory — the factory itself got this wrong — an issue text for its maintainers (`factory.sh lessons --issue lesson-finding-clean-code-test-duplication-extraneous`, sent by a person)
- c: not-now — asked again once the evidence has grown
- d: never — the same candidate does not come back

## Recommendation
a — instructions: no rule can see it; a line of the agent instructions tells the stages before they write it.
