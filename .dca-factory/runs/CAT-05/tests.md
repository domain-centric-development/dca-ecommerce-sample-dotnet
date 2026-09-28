# Tests — CAT-05

Mode: **adopt**. The plan found no test for any of the five scenarios, so each one has a characterization test:
green on today's code, asserting the scenario's `Then` with its values, plus a break under `breaks/`. No production
code and no existing test line was changed.

Carrier: done in this session, applying the `e2e-testing` craft (`carrier.test`) to the one browser test. The
profile names `knowledge: dca-knowledge`, but it was not consulted: adopting existing behaviour decides no pattern
question.

<!-- gate:tests -->
| criterion | test |
| --- | --- |
| logo-leads-home | DcaShop.E2eTests.SiteHeaderE2eTest#TheLogoLeadsFromAProductPageToTheHomePage |
| nav-home-opens-the-home-page | DcaShop.IntegrationTests.SiteLayoutTest#HomeInTheHeaderNavigationOpensTheHomePage |
| nav-products-opens-the-catalogue | DcaShop.IntegrationTests.SiteLayoutTest#ProductsInTheHeaderNavigationOpensTheCatalogue |
| footer-names-the-shop | DcaShop.IntegrationTests.SiteLayoutTest#TheFooterNamesTheShopAndLinksToTheEventLog |
| header-and-footer-on-the-home-page | DcaShop.IntegrationTests.SiteLayoutTest#TheHomePageShowsTheSameHeaderAndFooterAsTheCatalogue |

## Characterization
- DcaShop.E2eTests.SiteHeaderE2eTest#TheLogoLeadsFromAProductPageToTheHomePage
- DcaShop.IntegrationTests.SiteLayoutTest#HomeInTheHeaderNavigationOpensTheHomePage
- DcaShop.IntegrationTests.SiteLayoutTest#ProductsInTheHeaderNavigationOpensTheCatalogue
- DcaShop.IntegrationTests.SiteLayoutTest#TheFooterNamesTheShopAndLinksToTheEventLog
- DcaShop.IntegrationTests.SiteLayoutTest#TheHomePageShowsTheSameHeaderAndFooterAsTheCatalogue

## Files
- tests/DcaShop.E2eTests/SiteHeaderE2eTest.cs
- tests/DcaShop.E2eTests/Pages/BasePage.cs
- tests/DcaShop.E2eTests/Pages/HomePage.cs
- tests/DcaShop.IntegrationTests/SiteLayoutTest.cs
- tasks/CAT-05/breaks/DcaShop.E2eTests.SiteHeaderE2eTest--TheLogoLeadsFromAProductPageToTheHomePage.patch
- tasks/CAT-05/breaks/DcaShop.IntegrationTests.SiteLayoutTest--HomeInTheHeaderNavigationOpensTheHomePage.patch
- tasks/CAT-05/breaks/DcaShop.IntegrationTests.SiteLayoutTest--ProductsInTheHeaderNavigationOpensTheCatalogue.patch
- tasks/CAT-05/breaks/DcaShop.IntegrationTests.SiteLayoutTest--TheFooterNamesTheShopAndLinksToTheEventLog.patch
- tasks/CAT-05/breaks/DcaShop.IntegrationTests.SiteLayoutTest--TheHomePageShowsTheSameHeaderAndFooterAsTheCatalogue.patch

## Notes
- Page objects: `BasePage` gains `LogoTextAsync` and `FollowLogoAsync`. The header is on every page, so every page
  object can follow the logo. `HomePage` gains `OpenAsync`, which waits for the address `/` and the hero after a link
  was followed, beside the existing `NavigateToAsync`. Only lines were added. `HomePage.cs` already carried CAT-04's
  uncommitted additions, and those were left as they are.
- Browser test: the visitor reaches "Domain-Driven Design" through its catalogue card, because product ids are
  generated GUIDs. The test reads the logo's wordmark as rendered (`domaincentric<span>.commerce</span>` →
  "domaincentric.commerce"), clicks it, and asserts that the home page's heading "Welcome to domaincentric.commerce"
  shows.
- Integration tests mirror `HomePageTest`: `WebApplicationFactory<Program>`, links found by `data-test` inside
  `site-header` / `site-footer`, followed by their `href`. "The home page opens" is asserted as the followed page's
  `<h1>` reading "Welcome to domaincentric.commerce", and "the catalogue page opens" as "Our Products". The footer
  text is compared after tags are removed and entities decoded, so `&mdash;` must render as the em dash. "Same
  header and footer" is asserted as the home page's `site-header` and `site-footer` markup being identical to the
  catalogue's (same client, anonymous visitor), plus the "Home" and "Products" link texts.
- Breaks, all in `src/DcaShop.Web/Views/Shared/_Layout.cshtml`, one element each, chosen so that each one reddens
  only its own test among these five: the logo points to `/products`; "Home" points to `/products`; "Products"
  points to `/`; the footer's em dash becomes a hyphen on every page, so home and catalogue still match; the footer
  text is shortened on `/` only, so the catalogue footer stays right while home and catalogue differ.
- **Verified:** `dotnet build` shows 0 errors. The four `SiteLayoutTest` tests pass (4/4). `SiteHeaderE2eTest` passes
  together with CAT-04's `HomePageE2eTest` (2/2). `dotnet format` has run.
- **Not verified here:** the breaks. `git apply --check` and a helper script needed approval that was not given, so
  the patches were written by hand against the lines read from `_Layout.cshtml` (hunks at 20/23/24/64). The gate
  applies them to a scratch copy.
- **Shared-scenarios rule:** the browser test's `DisplayName` is the scenario key in words, "Logo leads home",
  because the story has no `Title:` lines. AGENTS.md requires that title in the specification's `scenarios.md`
  (`SharedScenariosTest`, only with `-p:SpecificationPath=…`), with a Java test under the same title. The
  specification was outside this stage's inputs, so the scenario still has to be added there. The user owns
  semantics.
- Unit tests: none. The plan names no domain invariant, and the story adopts page presentation only.
- Spikes: none written.
