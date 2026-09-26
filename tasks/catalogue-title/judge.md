# Judge — catalogue-title

## Verdict
verdict: pass

## Perspectives covered
- ddd: in-session (built-in description). The profile names `review-ddd` (`review.ddd:`), and this tool offers it,
  but it was not loaded because this session was limited to the stage's inputs. The change touches no domain type,
  event, repository or glossary term.
- hexagonal: in-session (built-in description). The profile names `review-hexagonal`, and this tool offers it, but
  it was not loaded for the same reason. The change adds no dependency, port, adapter logic or cross-context
  reference. It changes one string in a Razor view of Product's incoming web adapter.
- clean-code: in-session (built-in description). The profile names `review-clean-code`, and this tool offers it, but
  it was not loaded for the same reason.
- dca (added, `review.dca: dca-review`): not covered. The carrier was not run in this session, and nothing stands in
  for an added perspective. For the record, the diff contains no DCA construct: no domain, application, port or
  adapter code. The architecture suite passed 129 of 129 (`build.md`, Checks).
- knowledge (`dca-knowledge`, named in the profile): not consulted. None of the findings depends on an architecture
  pattern, rule or recipe.

## Confirmed defects
| Perspective | File:line | Severity | Defect | Fix |
| --- | --- | --- | --- | --- |

None.

## Considered and dropped
- `tests/DcaShop.IntegrationTests/CatalogueTest.cs` is in `story.diff` and `.verify/changed.txt`, but it is not this
  story's change. It is absent from `tree-before-plan.txt`, and no stage of this story lists it (`tests.md` and
  `build.md`, Files/Changed). Its break patches live under `tasks/CAT-01/breaks/`, so it belongs to the adopted
  story CAT-01, which ran in the same checkout during this story. It is not judged here. One note for CAT-01:
  `TheCatalogueIsTitledProductCatalog…` asserts the title "Product Catalog", so that adopted test depends on this
  story's view change.
- Dropping the heading assertion from the browser test departs from the plan's detail line ("the heading still
  reads 'Our Products' … asserted in the same happy-path test"). This is not a defect. A human decided it in
  `catalogue-title-01` (answer 2, applied 2026-09-26T06:48:12Z). `build.md`, "Deviations from the plan", records the
  deviation. The integration suite still asserts the heading (`ProductPageTest.cs:70`, per the decision record).
- The browser test's `DisplayName` has to be a scenario title in `../dca-sample-specification/scenarios.md`, and the
  Java suite needs a test with the same title (`SharedScenariosTest`). Whether the Java template already reads
  "Product Catalog" is also open. **This is a suspicion, not a finding**, carried over from round 1. The
  specification and the Java sample are outside this stage's inputs. The plan, the test stage and the build stage all
  record this as sync duty. `SharedScenariosTest` runs only with `-p:SpecificationPath`. The document stage or a
  human confirms it before the story is accepted.
- Test level: one browser test for the story's single happy-path criterion is the allowed level. The plan changes no
  adapter logic, so no integration test is owed, and no port is mocked.
- The breadcrumb assertion in the browser test is backed by the story's assumption (CAT-01-01). It reads the element
  through `data-test="breadcrumb"` (`ProductCatalogPage.cs`, `Breadcrumb` constant), as `project/product.md` "Look
  and feel" requires. It adds no navigation step and is not out of scope.
- `DocumentTitleAsync` reads `IPage.TitleAsync()`. The document title is not a page element, so no `data-test`
  applies. The browser drives the page and does not read the markup as text.
- `CatalogTitleE2eTest.cs` has no newline at the end of the file. `dotnet format --verify-no-changes` passed, so this
  is formatting only.
- `## Changed expectations` ("Products" → "Product Catalog"): the plan found no existing test that asserts the
  catalogue's document title, so no changed test is owed. The diff changes no existing test.

## Criteria re-checked
- catalogue-tab-reads-product-catalog: met.
  - Given: the seeded shop that the suite starts.
  - When: `ProductCatalogPage.NavigateToAsync(Page)` opens `/products`.
  - Then: the test compares the browser's document title exactly to "Product Catalog" (`CatalogTitleE2eTest.cs:20`).
  - Behaviour: the title comes from `Catalog.cshtml:2` (`ViewData["Title"] = "Product Catalog"`) through
    `_Layout.cshtml:9`, which adds no suffix. The test stage recorded the red proof against the pre-story title
    (`tests.md`, Notes: Expected "Product Catalog", Actual "Products").

## Previous round
- `ProductCatalogPage.cs:61-62` `HeadingAsync` found the `<h1>` by ARIA role instead of `data-test` (major): fixed.
  The human chose option 2 in decision `catalogue-title-01` (drop the heading assertion from the browser test). The
  current `ProductCatalogPage.cs` diff adds only `DocumentTitleAsync` and `BreadcrumbAsync`, and `BreadcrumbAsync`
  reads through `data-test`. `HeadingAsync` and its role locator are gone. `CatalogTitleE2eTest.cs:20-21` asserts
  only the title and the breadcrumb. `Catalog.cshtml` is unchanged except for line 2, so the views still match the
  Java templates.
