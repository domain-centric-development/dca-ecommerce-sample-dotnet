---
story: early-step-leads-to-the-open-step
epic: guest-checkout
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (7 rows, 0 without a fitting value)
---

# Findings — early-step-leads-to-the-open-step

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:165 | minor | introduced |  |  | the denied-step redirect repeats `Page`'s guard check (`:133`–`:143`) | extract `RedirectToOpenStepAsync(step, ct)` and call it from both | done | code | duplication | extraneous |
| 2 | ddd | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:164 | minor | introduced |  |  | comment says "step guard"; the glossary term is *Step access* | reword to "step access sends the customer to the step still open" | done | code | domain-term | incorrect |
| 3 | clean-code | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:214 | minor | introduced |  |  | `(Review, CheckoutStepNotCompletedException)` arm is only reachable with review granted, i.e. never | remove the arm or name the case that reaches it | done | code | dead-code | extraneous |
| 4 | hexagonal | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:171 | minor | introduced |  |  | the granted branch (re-render with message) has no test | add an adapter test with stubbed confirm and active-session ports, granted `StepAccess` | done | test | checking | missing |
| 5 | dca | tests/DcaShop.E2eTests/StepSentTooEarlyE2eTest.cs:12 | minor | introduced |  |  | title "Delivery sent before the buyer step" has no scenario in the shared specification | the user adds the scenario to the specification, or records the gap | done | story | traceability | missing |
| 6 | dca | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:162 | minor | introduced |  |  | the twin reaches the open step by another mechanism (Java redirects to the step's GET); rule 2's three questions are not answered | record catalog/rule/marker answers ("none" included) in WP-107 | done | plan | documentation | missing |
| 7 | clean-code | tests/DcaShop.IntegrationTests/StepSentTooEarlyTest.cs:55 | minor | introduced |  |  | `Step` takes `page` and `url`, always equal | drop `url`, post to `page` | done | test | interface | extraneous |
