---
id: the-review-fingerprint-has-the-reviews-name
epic: guest-checkout
kind: task
status: approved
created: 2026-10-29T15:12:47Z
context: checkout
title: The review fingerprint has the review's name
closes: []
depends_on: []
---

# The review fingerprint has the review's name

`OrderFingerprint` becomes `ReviewFingerprint`: it holds what the review showed, before any order exists. The
review itself becomes a value `CheckoutReview` the session works out and stores nowhere — the order as the review
shows it (lines at today's prices, buyer, address, shipping option, payment method, totals), with the operations
`Fingerprint` and the refusal unless the given fingerprint is its own — as the Java shop has it. The checkout
glossary gets `### Review (CheckoutReview)` and `### Review fingerprint (ReviewFingerprint)` in the places the Java
glossary has them; `Order fingerprint` goes.

A change no actor sees: no acceptance criterion, no test stage. It runs plan, build, tidy, judge and document, and
the existing suite, run whole, is its evidence; a test that existed before it stays as it is.
