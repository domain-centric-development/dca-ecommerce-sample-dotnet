---
id: the-cart-code-findings-are-closed
epic: shop-api
kind: task
status: draft
created: 2026-10-23T21:25:20Z
context: cart
title: The cart's open findings in its production code and documents are closed
closes: [remove-a-line-from-the-cart#1, remove-a-line-from-the-cart#2, remove-a-line-from-the-cart#3, the-cart-holds-no-more-than-is-in-stock#1, the-cart-holds-no-more-than-is-in-stock#2, the-cart-page#2, a-malformed-product-id-is-a-bad-request#1, a-malformed-product-id-is-a-bad-request#3, an-unusable-cart-request-is-a-problem-document#2, cart-api-answers-as-the-reference#2, cart-list-spells-the-status#4, the-cart-oversight-has-its-own-place-in-the-code#1, the-cart-oversight-has-its-own-place-in-the-code#2, the-cart-oversight-totals-at-todays-prices#1, logging-in-from-the-checkout-returns-to-it#2, logging-in-from-the-checkout-returns-to-it#3, only-the-browsers-own-guest-cart-is-taken-over#2, taking-over-the-guest-cart-is-a-step-of-its-own#2]
depends_on: [the-take-over-leads-only-to-pages-of-the-shop]
---

# The cart's open findings in its production code and documents are closed

Closes the open findings of the cart context in its production code and documents that change nothing an actor sees: test support copied instead of reused, test names and leftovers, weak test data, raw locators, doc comments a rule change made wrong, code the change left without a caller, a rule or literal in two places, names that drifted from the glossary, a twin divergence nobody recorded. Each row's fix stands in its findings row; the delivering gate sets them done.

A change no actor sees: no acceptance criterion, no test stage. A row that turns out to change behaviour is a `## needs-human` of the plan, and goes into a story of its own.

## Notes

- WP-151 decision 4 (the user, 2026-10-23): the open rows as clean-up items, one per bounded context, cart and checkout split into tests and code, after the pipeline closes new findings at their source (dca-factory 0.91.0); every item depends on `the-take-over-leads-only-to-pages-of-the-shop`.
