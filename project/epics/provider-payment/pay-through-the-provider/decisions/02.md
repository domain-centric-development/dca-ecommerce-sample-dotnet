---
id: pay-through-the-provider-02
story: pay-through-the-provider
stage: test
asked: 2026-09-26T06:27:23Z
---

# The new browser test has no scenario in the shared specification

## Question
The judge asked this question.

The change adds one browser test, `ProviderPaymentE2eTest.APaymentTheProviderAuthorizesLeadsOnToTheReview`, with
`[E2eFact(DisplayName = "A payment the provider authorizes leads on to the review")]`
(`tests/DcaShop.E2eTests/ProviderPaymentE2eTest.cs:23`). The sample's rule (`AGENTS.md`,
"Shared semantics since WP-39") says the browser suite implements the specification's `scenarios.md` and nothing
else: "a new end-user test therefore starts as a scenario in the specification, then lands in both suites under the
same title". `scenarios.md` has no payment-provider scenario. Its headings run from `scenario.catalog.seeded-in-name-order`
to `scenario.home.slider-no-auto-play`, and none is about payment. So `SharedScenariosTest.EveryScenarioIsOneBrowserTestAndEveryBrowserTestIsAScenario`
(`tests/DcaShop.UnitTests/Specification/SharedScenariosTest.cs:18`) fails on this test once it runs with
`-p:SpecificationPath=…` (`bound.Except(scenarios.Keys)` is not empty).

The specification belongs to you (its semantics are owned by the user and it is not part of the build), so no stage
may write the scenario. The plan and the test stage both flagged this as a sync duty and left it open. The Java twin
has the same gap: it has no REST payment provider and no `data-test="order-summary-total"` on its order summary. A
scenario in the specification would require the Java suite to add a test with the same title.

Everything else in the change is deliverable (see `tasks/pay-through-the-provider/judge.md`).

## Options
- a: **Add the scenario to the specification** under the test's title ("A payment the provider authorizes leads on to
  the review", or a title you prefer). The .NET test stage then renames the `DisplayName` to match where needed. The
  Java twin gets the scenario as open porting work (`planning/porting-status.md`) until its suite implements it.
- b: **Keep the happy path out of the shared scenarios.** Record in the sample that this test is .NET-only until the
  Java twin has a payment-provider adapter. That needs a sanctioned exception to the rule, because the rule as written
  has none, and `SharedScenariosTest` would have to learn about it.
- c: **Move the happy path to the integration level** (`ProviderPaymentTest.AnAuthorizedPaymentMovesOnToTheReviewAfterOneRequestForTheSessionTotal`
  already asserts it over HTTP) and drop the browser test. This goes against the story's happy-path mark and the
  answer to `pay-through-the-provider-01`.

## Recommendation
a. It keeps the rule as written and keeps the happy path in the browser, as the story and decision 01 want. The Java
side becomes a recorded porting item rather than a silent fork.

## Answer
answer: a — the scenario is in the specification now: scenario.checkout.provider-authorizes-payment, titled "A payment the provider authorizes moves the checkout on to the review step" (the Java suite's title, closer to the story's rule). Rename the browser test's DisplayName to that title; the Java twin carries the same title.
by: Christoph Bloemer
at: 2026-09-26T06:40:42Z
rationale: The browser suites implement the shared scenarios and nothing else; one title binds both.

## Applied
at: 2026-09-26T06:46:31Z
stage: test
