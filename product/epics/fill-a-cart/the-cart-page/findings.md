---
story: the-cart-page
epic: fill-a-cart
---

# Findings — the-cart-page

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | adopt | tests/DcaShop.E2eTests/CartPageE2eTest.cs:12 | minor | The display name "Cart shows lines with prices" is not a scenario in the shared specification, so `SharedScenariosTest` fails once the specification is switched on. | Add the scenario to the shared specification and to the Java suite under the same title. | open |
| 2 | adopt | product/epics/fill-a-cart/the-cart-page/story.md:52 | minor | 8.7799… rounds to 8.78 under any rounding mode, so this example cannot tell half-up from half-to-even. | Add an example that lands exactly on half a cent, or rename the criterion to what it proves. | open |
| 3 | clean-code | tests/DcaShop.IntegrationTests/CartTotalsTest.cs:61 | minor | `NewBrowser` and `AddOneUnitAsync` (:61-71) repeat `CartPageTest.cs:64-72`, and build the form inline instead of using `AddForm`. | Move both into a shared helper beside `AvailabilityPages` and call it from both classes. | done |
