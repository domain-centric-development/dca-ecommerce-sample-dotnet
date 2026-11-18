---
id: lesson-refusal-tests-green
kind: lesson
lesson: refusal
key: tests-green
stories: 12
asked: 2026-11-18T11:51:32Z
---

# The same gate refusal in 12 stories — tests-green

## Question
Counted, never guessed: this refusal repeats in 12 stories. Where does its correction belong? The factory writes no rule and no knowledge itself — the skill that owns the place does, with the evidence below. No story waits for this answer.

## Evidence
- a-refused-password-change-changes: a-refused-password-change-changes journal: integrate gate refused tests-green
- a-site-of-the-shops-own-can-embed-it: a-site-of-the-shops-own-can-embed-it journal: test gate refused tests-green
- an-assistant-reads-the-catalogue: an-assistant-reads-the-catalogue journal: adopt gate refused tests-green
- assistant-tools-name-their-server: assistant-tools-name-their-server journal: tidy gate refused tests-green
- choose-how-to-pay: choose-how-to-pay journal: test gate refused tests-green
- enter-the-buyer-details: enter-the-buyer-details journal: adopt gate refused tests-green
- keep-the-profile-up-to-date: keep-the-profile-up-to-date journal: test gate refused tests-green
- start-the-checkout-from-the-cart: start-the-checkout-from-the-cart journal: integrate gate refused tests-green
- stock-checks-answer-on-the-page: stock-checks-answer-on-the-page journal: build gate refused tests-green
- stock-warnings-in-the-cart: stock-warnings-in-the-cart journal: test gate refused tests-green
- the-cart-page: the-cart-page journal: test gate refused tests-green
- the-password-policy: the-password-policy journal: adopt gate refused tests-green

## Options
- a: factory — the factory itself got this wrong — an issue text for its maintainers (`factory.sh lessons --issue lesson-refusal-tests-green`, sent by a person)
- b: profile-check — a check the profile names, run at the gate (written by `/factory-setup`)
- c: instructions — a line of the project's agent instructions (written by `/dca-describe`)
- d: not-now — asked again once the evidence has grown
- e: never — the same candidate does not come back

## Recommendation
a — factory: the gate refuses the same check story after story; either the stages are not told what it holds, or the check refuses wrongly — the factory's maintainers look at both.
