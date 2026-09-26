# Judge — not-found-title

## Verdict
verdict: pass

## Perspectives covered
- ddd: `review-ddd` (profile `review.ddd`). No finding. No domain type, event, repository or glossary term is
  touched. "Not-found page" is UI wording in the web host, and that host belongs to no context (plan, Open assumptions).
- hexagonal: `review-hexagonal` (profile `review.hexagonal`). No finding. The change is one Razor view of the web
  host (`src/DcaShop.Web/Views/Error/404.cshtml`) plus test code. There is no port, use case, adapter, import or
  failure translation in it.
- clean-code: `review-clean-code` (profile `review.clean-code`). One minor finding, listed below.
- dca (added, `reviews: dca`): `dca-review` (profile `review.dca`). No DCA-layer file is in scope: the view is web-host
  markup and the other two files are E2E tests. Twin checkpoint: the change brings the .NET shop in line with the
  Java shop, whose not-found page already falls back to the layout's shop name (CAT-03-01, cited in the plan). Java
  needs no production change.
- Knowledge: the profile names `dca-knowledge`. It was not asked, because no finding turned on a pattern question.

## Confirmed defects
| Perspective | File:line | Severity | Defect | Fix |
| --- | --- | --- | --- | --- |
| clean-code | tests/DcaShop.E2eTests/Pages/NotFoundPage.cs:24 | minor | `DocumentTitleAsync() => Page.TitleAsync()` repeats `ProductCatalogPage.DocumentTitleAsync` (`Pages/ProductCatalogPage.cs:58`) word for word. That makes two copies, below the rule of three, so it does not block. | When a third page object needs the title, move `DocumentTitleAsync` to `BasePage` and delete the copies. |

## Considered and dropped
- The E2E test does not assert the error page's body ("404", heading, message, links). The story keeps the body
  unchanged as an answered assumption (CAT-03-01), and the story makes no claim about it. `ShopFlowTest.UnknownPageRendersThe404Page`
  still asserts the 404 status and `error-page__code`, as the plan names. So this is not a coverage gap.
- No integration test for the view: it passes through the existing `ShopFlowTest.UnknownPageRendersThe404Page`. No
  port or adapter changed, so the test-level rules have nothing to require. The one browser test covers the story's
  happy path, which is the level the plan chose.
- `NotFoundPage` uses `WaitForUrlAsync` and `WaitForAsync`. Both are in the committed `BasePage`
  (`HEAD:tests/DcaShop.E2eTests/Pages/BasePage.cs:17,25`). The change does not depend on the other stories'
  uncommitted `BasePage` edits.
- Neither new test file ends with a newline: that is formatting. `dotnet format --verify-no-changes` passed
  (build.md).
- Shared scenario and Java twin test (a suspicion, not a finding: this is outside the judge's inputs). AGENTS.md
  says a new end-user test starts as a scenario in `dca-sample-specification/scenarios.md` and lands in both suites
  under the same title. Neither is part of this diff, and the specification was not read (plan, Open assumptions).
  `SharedScenariosTest` checks this when it runs with `-p:SpecificationPath=…`. Before release, a human should
  confirm that the scenario "The not-found page is titled with the shop's name" exists there and has its Java test.
- Product and technical descriptions: the not-found page is not named in `project/product.md` or `project/tech.md`.
  The change adds no surface, state, persistence or integration, and nothing under "Not part of the product".

## Criteria re-checked
- not-found-tab-reads-the-shop-name: met.
  - Given: the ids are `{productId:guid}` (plan), so no seeded product can be `no-such-product`.
  - When: `NotFoundTitleE2eTest.cs:17` opens `/products/no-such-product` in the browser.
  - Then: `:19` asserts the title is exactly `"domaincentric.commerce"`.
  - Behaviour: with `404.cshtml:1` removed, `_Layout.cshtml:9` falls back to that literal.
  - The `DisplayName` is the scenario title, verbatim.
  - The "Changed expectations" line ("Page Not Found" → "domaincentric.commerce") is the whole diff. No existing
    test asserted the old title, so the plan has no `## Changed tests` section.
