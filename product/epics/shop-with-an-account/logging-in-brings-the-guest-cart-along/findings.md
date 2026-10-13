---
story: logging-in-brings-the-guest-cart-along
epic: shop-with-an-account
---

# Findings — logging-in-brings-the-guest-cart-along

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | plan | tests/DcaShop.E2eTests/GuestCartLoginE2eTest.cs:15 | minor | DisplayName "Guest cart follows into the account" has no scenario in the shared specification; `SharedScenariosTest` fails once run with `-p:SpecificationPath` (plan, open assumption 1) | Add the scenario to the specification's `scenarios.md` and the Java suite under the same title | open |
| 2 | test | tests/DcaShop.IntegrationTests/CartRecoveryOnLoginTest.cs:52 | minor | The Given "opened `/login?returnUrl=/checkout/buyer`" is fetched at :50, but `LogInAsync` posts a form from plain `/login` with a hand-set `returnUrl`; the page's own carrying of `returnUrl` is not exercised | Take the token and `returnUrl` from the page fetched at :50 | open |
