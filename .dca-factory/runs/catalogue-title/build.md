# Build — catalogue-title

Repeat round after the test stage recorded a fresh red proof for the browser test as it now stands
(`tasks/catalogue-title/.verify/gate-test.070535.txt`: `tests-red` passed, and the expectation change is recorded
under decision `catalogue-title-01`). The previous build gate (`gate-build.065031.txt`) failed only `red-proof`, and
only the test stage could fix that. Nothing else was confirmed against the build, so this round changes no production
code and no test. It runs the profile's checks again on the current tree and rewrites this file.

## Changed
| File | Why |
| --- | --- |

This round changed no files. The production change is from round 1 and has not changed since:
`src/DcaShop.Web/Views/Product/Catalog.cshtml:2` is now `ViewData["Title"] = "Product Catalog"`. The test files
`tests/DcaShop.E2eTests/CatalogTitleE2eTest.cs` and `tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs` are the
versions the test stage proved red. The previous gate's `files-listed` note named all three as "listed but not changed
by this stage", so they are not listed in the table.

## Criteria
- catalogue-tab-reads-product-catalog: met. The catalogue view sets the document title to exactly "Product Catalog",
  and the shared layout (`_Layout.cshtml:9`) writes it into `<title>` with no suffix. The browser test compares the
  tab title exactly, then checks that the breadcrumb still reads "Home / Products". It passes.

## Deviations from the plan
- The plan said the heading "Our Products" would be asserted in the same happy-path test. It is not, because the
  human answered `catalogue-title-01` with option 2. The integration suite covers the heading
  (`tests/DcaShop.IntegrationTests/ProductPageTest.cs:70`). The markup is unchanged, so the view still matches the
  Java template.

## Checks
- `dotnet build`: succeeded, 0 errors
- `dotnet test tests/DcaShop.UnitTests`: passed, 176 passed, 4 skipped
- `dotnet test tests/DcaShop.IntegrationTests`: passed, 67 passed, 1 skipped
- `dotnet test tests/DcaShop.ArchitectureTests`: passed, 129 of 129
- `dotnet test tests/DcaShop.E2eTests`: passed, 39 passed, 1 skipped (the suite started the shop itself)
- `dotnet format --verify-no-changes`: clean
- `dotnet format` (formatFix): run last. It changed no file; `git status` of `src/` and `tests/` is the same as
  before.

## Carriers and knowledge
- `carrier.build: dca-modelling`: not applied, because there is no tactical type to model. The stage was done
  in-session.
- `carrier.guard: dca-discipline`: applied to the one file this round wrote, this record. The story's only production
  change is a string in a Razor view of Product's incoming web adapter. It adds no framework type to the domain, no
  port, no cross-context reference and no event, so no invariant is affected.
- `knowledge: dca-knowledge`: not consulted, because no architecture question came up.
- Sync duty, recorded by the plan and the judge: the scenario title in `../dca-sample-specification/scenarios.md`
  and the Java template's title are outside this stage's inputs and were not checked.
