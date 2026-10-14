---
story: event-log-has-an-entry-per-listener
epic: run-the-shop
---

# Findings — event-log-has-an-entry-per-listener

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.E2eTests/RestartedShop.cs:8 | minor | `RestartedShop` only forwards to `SideShop.Start()`; its summary ("every store empty") contradicts `SideShop`'s ("seeded catalogue and stock") | call `SideShop.Start()` at the callers, move the cookie remark to `ShopRestartE2eTest`, drop `RestartedShop` | done |
| 2 | clean-code | tests/DcaShop.E2eTests/CartStockWarningE2eTest.cs:20 | minor | marked `[RestartingFact]` though it only starts a side shop | mark it `[OwnShopFact]` | done |
| 3 | dca | tests/DcaShop.E2eTests/BackofficeRetryE2eTest.cs:21 | minor | "Failed entry offers a retry" is no title in the specification's `scenarios.md`, and `SharedScenariosTest.cs:14` counts no `SideShopFactAttribute` (same gap as the two existing side-shop tests); the plan's assumption that the specification carries the title does not hold | add the scenario to the specification (the user's call) and let `TestAttribute` match `OwnShopFact`/`RestartingFact` | open |
