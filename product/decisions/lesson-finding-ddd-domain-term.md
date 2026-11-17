---
id: lesson-finding-ddd-domain-term
kind: lesson
lesson: finding
key: ddd/domain-term
stories: 3
asked: 2026-11-11T04:03:16Z
---

# The same finding in 3 stories — ddd/domain-term

## Question
Counted, never guessed: this finding repeats in 3 stories. Where does its correction belong? The factory writes no rule and no knowledge itself — the skill that owns the place does, with the evidence below. No story waits for this answer.

## Evidence
- the-confirmation-page-marks-the-order-reference: the-confirmation-page-marks-the-order-reference#1
- the-pricing-findings-are-closed: the-pricing-findings-are-closed#1
- the-checkout-code-findings-are-closed: the-checkout-code-findings-are-closed#2

## Options
- a: project-rule — an architecture rule of the project's own, so the suite catches it (written by the method's skills (`/dca-modelling`))
- b: profile-check — a check the profile names, run at the gate (written by `/factory-setup`)
- c: knowledge-node — a knowledge node of the project's own (written by the method's skills (`/dca-modelling`))
- d: rule-library — the method's rule suite could catch this — an issue text for its maintainers (`factory.sh lessons --issue lesson-finding-ddd-domain-term`, sent by a person)
- e: factory — the factory itself got this wrong — an issue text for its maintainers (`factory.sh lessons --issue lesson-finding-ddd-domain-term`, sent by a person)
- f: not-now — asked again once the evidence has grown
- g: never — the same candidate does not come back

## Recommendation
a — project-rule: the judge finds the same flaw story after story; a rule catches it at the gate instead.
