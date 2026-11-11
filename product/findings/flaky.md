---
findings: flaky
---

# Findings — flaky

Findings that belong to no one story, kept for a task or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`, a task closes a row with `closes: flaky#<n>`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | flaky | DcaShop.IntegrationTests.ProductCreationTest#ACreatedProductJoinsTheCatalogueWithItsPriceAndStock | minor | The test DcaShop.IntegrationTests.ProductCreationTest#ACreatedProductJoinsTheCatalogueWithItsPriceAndStock fails now and then in the whole suite, in more than one story — it is fixed, not run again | red in a whole suite run and green on its own run, in the-checkout-code-findings-are-closed, a-refusal-after-the-authorization-cancels-it, checkout-refusals-say-what-happened, the-pricing-findings-are-closed | find what it depends on — load, order, a timeout, shared state — and fix the test or the code, as a task | open |
