---
id: lesson-refusal-files-listed
kind: lesson
lesson: refusal
key: files-listed
stories: 14
asked: 2026-11-18T11:51:32Z
---

# The same gate refusal in 14 stories — files-listed

## Question
Counted, never guessed: this refusal repeats in 14 stories. Where does its correction belong? The factory writes no rule and no knowledge itself — the skill that owns the place does, with the evidence below. No story waits for this answer.

## Evidence
- a-place-order-with-the-review-open-asks-for-it: a-place-order-with-the-review-open-asks-for-it journal: build gate refused files-listed
- a-refused-password-change-changes: a-refused-password-change-changes journal: test gate refused files-listed
- an-early-step-says-what-is-still-open: an-early-step-says-what-is-still-open journal: test gate refused files-listed
- an-open-checkout-keeps-its-price: an-open-checkout-keeps-its-price journal: test gate refused files-listed
- enter-the-buyer-details: enter-the-buyer-details journal: test gate refused files-listed
- event-log-has-an-entry-per-listener: event-log-has-an-entry-per-listener journal: test gate refused files-listed
- logging-in-from-the-checkout-returns-to-it: logging-in-from-the-checkout-returns-to-it journal: test gate refused files-listed
- remove-a-line-from-the-cart: remove-a-line-from-the-cart journal: build gate refused files-listed
- stock-checks-answer-on-the-page: stock-checks-answer-on-the-page journal: build gate refused files-listed
- the-buyer-gets-an-order-confirmation: the-buyer-gets-an-order-confirmation journal: test gate refused files-listed
- the-checkout-tests-findings-are-closed: the-checkout-tests-findings-are-closed journal: build gate refused files-listed
- the-event-log-lists-every-delivery: the-event-log-lists-every-delivery journal: test gate refused files-listed
- the-payment-is-authorized-at-place-order: the-payment-is-authorized-at-place-order journal: build gate refused files-listed
- the-steps-show-the-progress: the-steps-show-the-progress journal: test gate refused files-listed

## Options
- a: factory — the factory itself got this wrong — an issue text for its maintainers (`factory.sh lessons --issue lesson-refusal-files-listed`, sent by a person)
- b: profile-check — a check the profile names, run at the gate (written by `/factory-setup`)
- c: instructions — a line of the project's agent instructions (written by `/dca-describe`)
- d: not-now — asked again once the evidence has grown
- e: never — the same candidate does not come back

## Recommendation
a — factory: the gate refuses the same check story after story; either the stages are not told what it holds, or the check refuses wrongly — the factory's maintainers look at both.
