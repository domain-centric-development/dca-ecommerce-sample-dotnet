---
story: back-office-guards-every-address
epic: run-the-shop
---

# Findings — back-office-guards-every-address

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | dca | src/DcaShop.Backoffice/Adapter/Incoming/Web/EventPublicationPageController.cs:88 | minor | The catch-all is `[HttpGet]` only, so a POST/PUT/DELETE to an unknown address below `/backoffice` is answered (405 or not-found) without the operator scheme, while the Java twin guards `/backoffice/**` for every verb; the criterion only opens an address in a browser and the plan states the GET-only assumption (plan.md:31). | Use `[Route("{**address}")]` instead of `[HttpGet("{**address}")]`, so the class's `[Authorize]` answers every verb first. | open |
| 2 | dca | tests/DcaShop.E2eTests/BackofficeE2eTest.cs:79 | minor | The browser test's title "Login after a redirect opens the event log" is no scenario in the shared specification's `scenarios.md` (the backoffice scenarios there end at `scenario.backoffice.event-payload`), and the Java suite has no test of that title, so `SharedScenariosTest` with `-p:SpecificationPath` would name it as a test without a scenario. Earlier delivered factory stories (e.g. `ShopFitsAPhoneE2eTest.cs:19`) share the same gap; the specification is user-owned and outside this checkout. | Add the scenario (e.g. `scenario.backoffice.unknown-address-redirect`, this title) to the specification and the same-titled test to the Java `BackofficeE2ETest`. | done |
| 3 | dca | src/DcaShop.Backoffice/Adapter/Incoming/Web/EventPublicationPageController.cs:88 | minor | The plan does not record the three harness questions (catalog node, rule, marker) for an architectural change that places the prefix guard differently from the Java twin (a controller catch-all here, a path-scoped security matcher there). | Record the answer in the WP or an ADR — at least a catalog pitfall "an unmatched address below a guarded prefix bypasses an endpoint-level authorization". | open |
| 4 | clean-code | tests/DcaShop.E2eTests/BackofficeE2eTest.cs:83 | minor | `"/backoffice/settings"` does not say that the point is an address no action answers; a later real settings page would silently stop the test from covering the catch-all. | Name it, e.g. `private const string UnknownBackofficeAddress = "/backoffice/settings";`. | done |
