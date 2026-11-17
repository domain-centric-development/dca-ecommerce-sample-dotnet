---
story: each-payment-names-its-checkout
epic: provider-payment
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — each-payment-names-its-checkout

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.IntegrationTests/PaymentReferenceTest.cs:28 | minor | introduced |  |  | The class is named after the payment reference, which the glossary uses for the reference the provider answers with (`product/spec/checkout/glossary.md:537`). The class tests the checkout reference the shop sends. | Rename the class and its file to `CheckoutReferenceTest`. | open | test | domain-term | incorrect |
