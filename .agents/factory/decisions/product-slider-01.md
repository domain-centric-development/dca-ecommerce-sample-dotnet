---
id: product-slider-01
story: product-slider
stage: test
asked: 2026-09-25T10:51:54Z
---

# The scenario title with ASCII double quotes cannot be matched by the two source readers of browser-test display names

## Question
`scenario.home.slider-below-hero` is titled `The homepage shows a "Discover products" slider directly below the hero`
(`../dca-sample-specification/scenarios.md:187`). The browser test carries it as the xUnit `DisplayName`, which in C#
can only be written with escaped quotes: `DisplayName = "The homepage shows a \"Discover products\" slider directly below the hero"`
(`tests/DcaShop.E2eTests/HomeSliderE2eTest.cs`). At run time the display name equals the title exactly (the TRX report
shows it unescaped), but both readers that take the display name from the *source text* keep the backslashes:

- the story gate's `display_name_of` (`.agents/factory/story-gate.py:1239-1241`, regex `DisplayName\s*=\s*"((?:[^"\\]|\\.)*)"`)
  reads `The homepage shows a \"Discover products\" slider…`, finds no such case in the report and fails
  `tests-red` for `shows-discover-products-slider-below-hero` ("no test report from this run shows it ran");
- `SharedScenariosTest` (`tests/DcaShop.UnitTests/Specification/SharedScenariosTest.cs:15,33`, same regex, run with
  `-p:SpecificationPath=…`) reports the scenario as having no test and the test as having no scenario.

No C# string literal form carries `"` without an escape that both regexes accept (verbatim `@"…""…"` and raw
`"""…"""` literals are not matched by `DisplayName\s*=\s*"` at all). The test stage may not change the gate (pipeline
machinery), may not change `SharedScenariosTest` (a test that existed before the story and is not in the plan's
`## Changed tests`), and may not change the specification (the user owns semantics). All 13 other criteria pass the
test gate's red check.

## Options
- a: Unescape in the readers — `SharedScenariosTest` turns `\"` (and `\\`) into `"` (`\\`) before comparing, listed as a
  changed test for this story; and the dca-factory gate's `display_name_of` does the same upstream (dca-marketplace),
  then `/factory-update`. Keeps the title as agreed; costs a pipeline release before this story's test gate passes.
- b: Change the scenario title in the specification so it has no ASCII double quotes (e.g. `The homepage shows a
  slider headed Discover products directly below the hero`, or typographic quotes `“Discover products”`, which C#
  writes without escape); the test's DisplayName follows. No tool change; the Java twin takes the same title later.
- c: Accept the gate failure for this one criterion for now (the test is red for the right reason — verified by the
  runner's own report) and carry the reader fix as a follow-up.

## Recommendation
b with typographic quotes is the cheapest and keeps the wording; a is the lasting fix, since any future title with a
quote hits the same gap (a determinism gap in the harness, AGENTS.md principle 1) — worth doing either way.

## Answer
answer: a
by: shop-owner
at: 2026-09-25T12:06:06Z
rationale: the readers are fixed, the title stays as agreed. The dca-factory gate unescapes the literal since 0.33.4 (already in this project's `.agents/factory/`). `SharedScenariosTest` in this sample does the same — turns `\"` into `"` and `\\` into `\` before comparing — listed as a changed test of this story. The Java counterpart follows with its own story (TODO #88).

## Applied
at: 2026-09-25T12:07:26Z
stage: test
