---
story: starting-and-placing-check-the-cart
epic: guest-checkout
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (1 rows, 0 without a fitting value)
---

# Findings — starting-and-placing-check-the-cart

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.IntegrationTests/CheckoutStartFromTheCartTest.cs:20 | minor | introduced |  |  | Each test builds its own `WebApplicationFactory<Program>` (also :38), though the two use different products (Hexagon, Domain-Driven Design) and one host per class would do, as the sibling tests do | Take the host as `IClassFixture<WebApplicationFactory<Program>>`, like `PlaceOrderRefusalPageTest` | done | test | duplication | extraneous |
