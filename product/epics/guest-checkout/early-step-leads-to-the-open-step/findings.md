---
story: early-step-leads-to-the-open-step
epic: guest-checkout
---

# Findings — early-step-leads-to-the-open-step

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | clean-code | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:165 | minor | the denied-step redirect repeats `Page`'s guard check (`:133`–`:143`) | extract `RedirectToOpenStepAsync(step, ct)` and call it from both | open |
| 2 | ddd | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:164 | minor | comment says "step guard"; the glossary term is *Step access* | reword to "step access sends the customer to the step still open" | open |
| 3 | clean-code | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:214 | minor | `(Review, CheckoutStepNotCompletedException)` arm is only reachable with review granted, i.e. never | remove the arm or name the case that reaches it | open |
| 4 | hexagonal | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:171 | minor | the granted branch (re-render with message) has no test | add an adapter test with stubbed confirm and active-session ports, granted `StepAccess` | open |
| 5 | dca | tests/DcaShop.E2eTests/StepSentTooEarlyE2eTest.cs:12 | minor | title "Delivery sent before the buyer step" has no scenario in the shared specification | the user adds the scenario to the specification, or records the gap | open |
| 6 | dca | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:162 | minor | the twin reaches the open step by another mechanism (Java redirects to the step's GET); rule 2's three questions are not answered | record catalog/rule/marker answers ("none" included) in WP-107 | open |
| 7 | clean-code | tests/DcaShop.IntegrationTests/StepSentTooEarlyTest.cs:55 | minor | `Step` takes `page` and `url`, always equal | drop `url`, post to `page` | open |
