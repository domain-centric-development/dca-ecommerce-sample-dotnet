---
id: the-order-changed-since-the-review-is-one-failure
epic: guest-checkout
kind: task
status: draft
created: 2026-10-29T15:12:47Z
context: checkout
title: The order changed since the review is one failure
closes: []
depends_on: []
---

# The order changed since the review is one failure

`PriceChangedSinceReview` and `OrderChangedSinceReview` become one domain failure `OrderChangedSinceReviewException`
that names the product when only a price differs — the shape both shops agreed on (2026-10-29). The messages the
buyer sees do not change. The checkout glossary's entry is headed
`### OrderChangedSinceReview (OrderChangedSinceReviewException)` and stands after `CheckoutValidation`, where the Java
glossary has it.

A change no actor sees: no acceptance criterion, no test stage. It runs plan, build, tidy, judge and document, and
the existing suite, run whole, is its evidence; a test that existed before it stays as it is.
