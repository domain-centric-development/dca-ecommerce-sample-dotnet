# Tests — catalogue-title

Repeat round. The build gate failed `red-proof` because the browser test changed after round 1 saw it fail: decision
`catalogue-title-01`, option 2, dropped the heading assertion. This round records a red proof for the test as it
stands now. It changes no test and no production code.

<!-- gate:tests -->
| criterion | test |
| --- | --- |
| catalogue-tab-reads-product-catalog | DcaShop.E2eTests.CatalogTitleE2eTest#CatalogPageIsTitledProductCatalog |

## Files
- tests/DcaShop.E2eTests/CatalogTitleE2eTest.cs
- tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs

## Notes
- DcaShop.E2eTests.CatalogTitleE2eTest#CatalogPageIsTitledProductCatalog fails on
  `Assert.Equal("Product Catalog", await catalog.DocumentTitleAsync())` (`CatalogTitleE2eTest.cs:20`) when the page
  has the pre-story title: Expected "Product Catalog", Actual "Products". The build stage already changed the view,
  so to see this failure I set `Catalog.cshtml:2` back to `ViewData["Title"] = "Products"` for one run
  (`dotnet test tests/DcaShop.E2eTests --filter FullyQualifiedName~CatalogTitleE2eTest`: 1 failed, 0 passed). Then I
  restored `"Product Catalog"`. `git diff` of the view is the build stage's one-line change again. The test fails on
  its assertion, not on wiring: the suite started the shop, Playwright opened `/products`, and the browser read
  the document title.
- The test as it is now, following decision `catalogue-title-01` (option 2, Christoph Bloemer): it asserts the
  document title exactly, then that the breadcrumb still reads "Home / Products" (story assumption, CAT-01-01). It no
  longer asserts the heading "Our Products". `tests/DcaShop.IntegrationTests/ProductPageTest.cs:70` covers the
  heading, and that file is unchanged.
- Page object `ProductCatalogPage`: `DocumentTitleAsync` (`IPage.TitleAsync()`) and `BreadcrumbAsync` (the existing
  `data-test="breadcrumb"`, whitespace normalised). `HeadingAsync`, which used a role locator, was removed in the
  build round that applied the decision. The uncommitted `ViewProductAsync(string)` from another story is left as it
  was.
- No unit tests: the plan names no invariant, because the change is presentation only.
- No integration test: the plan lists no changed adapter and assigns the one criterion to the e2e level.
- No stubs were needed.
- Scenario sync (unchanged from round 1): the `DisplayName` "The catalogue page is titled \"Product Catalog\"" has to
  be a scenario title in `../dca-sample-specification/scenarios.md` and in the Java suite (`SharedScenariosTest`, run
  only with `-p:SpecificationPath`). That is sync duty outside this stage's inputs and was not checked.
- Carrier: `carrier.test: e2e-testing` is named, but its skill was not loaded in this round because no test was
  written. The stage was done in-session. `dca-knowledge` was not consulted because no architecture question came
  up.
- `dotnet format --verify-no-changes`: clean. `formatFix` was not run because this round changed no source file.
