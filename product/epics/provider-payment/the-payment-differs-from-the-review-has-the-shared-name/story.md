---
id: the-payment-differs-from-the-review-has-the-shared-name
epic: provider-payment
kind: task
status: draft
created: 2026-10-29T15:12:47Z
context: checkout
title: The payment differs from the review has the shared name
closes: []
depends_on: []
---

# The payment differs from the review has the shared name

`PaymentDoesNotCoverTotalException` becomes `PaymentDiffersFromReviewException`: the check is exact — the provider
the review showed and exactly its total — and "cover" reads as "at least". The checkout glossary's entry is headed
`### PaymentDiffersFromReview (PaymentDiffersFromReviewException)` and stands after the order changed since the
review, where the Java glossary has it; the entry `Authorized amount` names it.

A change no actor sees: no acceptance criterion, no test stage. It runs plan, build, tidy, judge and document, and
the existing suite, run whole, is its evidence; a test that existed before it stays as it is.
