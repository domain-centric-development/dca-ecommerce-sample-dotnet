---
id: the-checkout-code-findings-are-closed
epic: provider-payment
kind: task
status: approved
created: 2026-10-23T21:25:20Z
context: checkout
title: The checkout's open findings in its production code and documents are closed
closes: [a-place-order-with-the-review-open-asks-for-it#1, an-open-checkout-keeps-its-price#2, an-open-checkout-keeps-its-price#3, an-open-checkout-keeps-its-price#4, paying-again-requests-no-second-payment#1, paying-again-requests-no-second-payment#2, paying-again-requests-no-second-payment#3, the-payment-is-authorized-at-place-order#3, the-payment-is-authorized-at-place-order#5, the-payment-is-authorized-at-place-order#6, the-payment-is-authorized-at-place-order#7, logging-in-from-the-checkout-returns-to-it#5, two-orders-never-sell-more-than-the-stock#2]
depends_on: [the-take-over-leads-only-to-pages-of-the-shop]
---

# The checkout's open findings in its production code and documents are closed

Closes the open findings of the checkout context in its production code and documents that change nothing an actor sees: test support copied instead of reused, test names and leftovers, weak test data, raw locators, doc comments a rule change made wrong, code the change left without a caller, a rule or literal in two places, names that drifted from the glossary, a twin divergence nobody recorded. Each row's fix stands in its findings row; the delivering gate sets them done.

A change no actor sees: no acceptance criterion, no test stage. A row that turns out to change behaviour is a `## needs-human` of the plan, and goes into a story of its own.

## Notes

- WP-151 decision 4 (the user, 2026-10-23): the open rows as clean-up items, one per bounded context, cart and checkout split into tests and code, after the pipeline closes new findings at their source (dca-factory 0.91.0); every item depends on `the-take-over-leads-only-to-pages-of-the-shop`.
- two-orders-never-sell-more-than-the-stock#2 moved here from `checkout-refusals-say-what-happened` on the user's say-so (2026-10-24): the "… item(s) unavailable or out of stock" notice is reached by no path — no check produces a price-change error — so the fix is removing the unreachable notice and its branch, not rewording it.
- paying-again-requests-no-second-payment#2, the word (the user, 2026-10-24): "payment cancellation" stays what the provider does; "release" enters the glossary as the session's side of it — the session gives up its payment selection; "forget" goes.
- the-payment-is-authorized-at-place-order#2 moved to `a-refusal-after-the-authorization-cancels-it` on the user's say-so (2026-10-24): that story rewrites the same glossary entry the other way.
- Twin: an-open-checkout-keeps-its-price#4 (renaming `CheckoutPricing`) and the-payment-is-authorized-at-place-order#7 (the review's payment-reference branch) name the Java sample too; it needs the same change as its own backlog item there. This task changes this shop and its glossary only.
