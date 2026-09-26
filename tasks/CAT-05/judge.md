# Judge — CAT-05

## Verdict
verdict: pass

## Perspectives covered
Adopt mode (`status: adopted`). No production code was built, so only one thing is reviewed: whether each mapped
test proves its scenario and whether its break reddens it for a reason the test depends on. The review was done
in-session.
- ddd: not run. Adopt mode leaves nothing to review from this perspective, because no model code changed.
  (`review-ddd` is named and available.)
- hexagonal: not run for the same reason. The diff touches only `tests/`. (`review-hexagonal` is named and available.)
- clean-code: in-session, applied to the added test code only.
- dca (added, carrier `dca-review`): not run. The change has no production code for it to review.
- The `dca-knowledge` knowledge source was not consulted: no question here needed a pattern decided.

## Confirmed defects
| Perspective | File:line | Severity | Defect | Fix |
| --- | --- | --- | --- | --- |
| — | — | — | none | — |

## Considered and dropped
- Hand-written breaks, which `tests.md` says were never applied: I compared each hunk against
  `src/DcaShop.Web/Views/Shared/_Layout.cshtml`. Every hunk's context and removed line match the current file:
  lines 20–26 for the logo, 23–29 for Home, 24–30 for Products and 64–70 for the footer. Each break alters the one
  thing its test asserts:
  - logo `href` → `/products`: `FollowLogoAsync` waits for `/` and the hero, which does not happen.
  - Home `href` → `/products`: the `<h1>` becomes "Our Products".
  - Products `href` → `/`: the `<h1>` becomes the home heading.
  - The em dash becomes a hyphen: the footer text no longer matches. The footer is still the same on every page,
    so the equality test stays green.
  - The footer is shortened on `/` only: the home footer and the catalogue footer differ, while the catalogue
    footer that `TheFooterNamesTheShopAndLinksToTheEventLog` reads stays correct.

  No break changes something its test does not depend on. Whether they apply cleanly is still for the gate to
  confirm: `git apply --check` was not approved in this session.
- `SiteLayoutTest.cs:26`, `:39`: the tests use `GET /products` and `GET /` for "a visitor on the catalogue page"
  and "a visitor on the home page" without asserting which page loaded. Not a defect, for two reasons. The Given
  is just a request to the route, and `CatalogueTest` and `HomePageTest` already cover those pages' headings.
- `SiteLayoutTest.cs:74`: "the same header" is checked by comparing the header markup as a string. That is strict,
  but the story says "the same header", and the comparison uses one anonymous client for both pages. It is correct
  as written, not brittle beyond what the criterion asks.
- `SiteHeaderE2eTest.cs:12`, `DisplayName = "Logo leads home"`: `SharedScenariosTest` requires that title in the
  specification's `scenarios.md` and a Java test with the same title, but only when
  `-p:SpecificationPath=…` is set. I did not read the specification because it is outside this stage's inputs.
  The test stage recorded this as open. It is a sync duty for the user, who owns the semantics. It is not
  something this stage can confirm as a defect, so it is recorded here without blocking.
- Test levels: only `logo-leads-home`, the story's `happy-path`, runs in the browser. The other four are
  integration tests through `WebApplicationFactory<Program>`. None of them reads markup as text in place of the
  browser for a behaviour only the browser shows: link targets, headings and footer text are all HTTP-visible.
  There is nothing to find here.
- Test code (clean-code): the helpers `Header`, `Footer`, `Element`, `Link`, `Match` and `Rendered` are named for
  what they return, and none of them is duplicated. `BasePage.FollowLogoAsync` and `HomePage.OpenAsync` are purely
  added lines.

## Criteria re-checked
- logo-leads-home: met. `SiteHeaderE2eTest.cs:15-21`:
  - Given: it opens the "Domain-Driven Design" product page through its catalogue card.
  - When: it asserts the logo reads "domaincentric.commerce" and clicks it.
  - Then: it waits for `/` and the hero, then asserts the heading "Welcome to domaincentric.commerce".
- nav-home-opens-the-home-page: met. `SiteLayoutTest.cs:22-32`:
  - Given: the catalogue page.
  - When: it finds `nav-home-link` inside `site-header`, asserts its text "Home" and follows it.
  - Then: it asserts the home page's `<h1>`.
- nav-products-opens-the-catalogue: met. `SiteLayoutTest.cs:35-45`:
  - Given: the home page.
  - When: it finds the header link "Products" and follows it.
  - Then: it asserts the `<h1>` "Our Products".
- footer-names-the-shop: met. `SiteLayoutTest.cs:48-61`:
  - Then: the footer text reads "domaincentric.commerce — Built with Domain-Centric Architecture". The entity is
    decoded before the comparison, so the em dash is checked.
  - And: the link text is "Event Log" and its target is `/backoffice/events`.
- header-and-footer-on-the-home-page: met. `SiteLayoutTest.cs:64-75`:
  - the home header offers "Home" and "Products";
  - the home header markup equals the catalogue's;
  - the home footer markup equals the catalogue's.
