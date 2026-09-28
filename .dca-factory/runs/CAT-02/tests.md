# Tests — CAT-02

Mode: **adopt**. The plan found no existing test for any of the five scenarios (only the image partly, from the
homepage slider). So each one has a characterization test: green on today's code, asserting the scenario's
`Then` with its values, plus a break under `breaks/`. No production code and no existing test was changed.

Carrier: done in this session with the `e2e-testing` craft (`carrier.test`) applied to the one browser test. The
profile names `knowledge: dca-knowledge`, but it was not consulted: adopting existing behaviour decides no pattern
question.

<!-- gate:tests -->
| criterion | test |
| --- | --- |
| view-details-opens-the-product-page | DcaShop.E2eTests.ProductPageE2eTest#ViewDetailsOnACatalogueCardOpensThatProductsPage |
| product-page-shows-image-description-and-category | DcaShop.IntegrationTests.ProductPageTest#TheProductPageShowsTheImageTheDescriptionAndTheCategory |
| product-page-title-is-the-product-name | DcaShop.IntegrationTests.ProductPageTest#TheBrowserTabTitleIsTheProductName |
| product-page-breadcrumb | DcaShop.IntegrationTests.ProductPageTest#TheBreadcrumbLeadsFromHomeOverProductsToTheProduct |
| back-to-products-returns-to-the-catalogue | DcaShop.IntegrationTests.ProductPageTest#BackToProductsReturnsToTheCatalogue |

## Characterization
- DcaShop.E2eTests.ProductPageE2eTest#ViewDetailsOnACatalogueCardOpensThatProductsPage
- DcaShop.IntegrationTests.ProductPageTest#TheProductPageShowsTheImageTheDescriptionAndTheCategory
- DcaShop.IntegrationTests.ProductPageTest#TheBrowserTabTitleIsTheProductName
- DcaShop.IntegrationTests.ProductPageTest#TheBreadcrumbLeadsFromHomeOverProductsToTheProduct
- DcaShop.IntegrationTests.ProductPageTest#BackToProductsReturnsToTheCatalogue

## Files
- tests/DcaShop.E2eTests/ProductPageE2eTest.cs
- tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs
- tests/DcaShop.IntegrationTests/ProductPageTest.cs
- tasks/CAT-02/breaks/DcaShop.E2eTests.ProductPageE2eTest--ViewDetailsOnACatalogueCardOpensThatProductsPage.patch
- tasks/CAT-02/breaks/DcaShop.IntegrationTests.ProductPageTest--TheProductPageShowsTheImageTheDescriptionAndTheCategory.patch
- tasks/CAT-02/breaks/DcaShop.IntegrationTests.ProductPageTest--TheBrowserTabTitleIsTheProductName.patch
- tasks/CAT-02/breaks/DcaShop.IntegrationTests.ProductPageTest--TheBreadcrumbLeadsFromHomeOverProductsToTheProduct.patch
- tasks/CAT-02/breaks/DcaShop.IntegrationTests.ProductPageTest--BackToProductsReturnsToTheCatalogue.patch

## Notes
- `ProductCatalogPage.ViewProductAsync(string name)` is new. It follows "View Details" on the card with that exact
  title. Existing page-object members are unchanged.
- The integration tests find the product the way a visitor does: the "View Details" target of its catalogue card
  (`GET /products`). They do not use a repository lookup.
- The image assertion checks `src` `/images/products/ddd-book.webp` and `alt` "Domain-Driven Design" inside
  `product-detail`. It also checks that the image is served with status 200 and an `image/*` content type.
- The breadcrumb assertion reads the texts of the breadcrumb's links, separators and current item in order. It checks
  the targets `/` (Home) and `/products` (Products), and that `/products` renders "Our Products". Per the plan, what
  the home page shows is left to `CAT-04`.
- Verified here: all four integration tests are green on today's code. With each Detail break applied (all four at
  once, since they touch separate elements), each test was red **on its own assertion**:
  category "Books" vs "Domain-Driven Design"; title "Clean Architecture" vs "Product"; breadcrumb target "/products"
  vs "/"; back link: the followed page has no `product-grid`. The tree was reverted afterwards (`git status src` is
  clean).
- **Gate report `tests-green` (browser test skipped):** fixed without changing the test. Since commit 5c2bb69 the
  browser suite starts the shop itself, in the test process on a free port (`ShopUnderTest`), and `[E2eFact]` no
  longer skips when `E2E_BASE_URL` is unset. `dotnet test tests/DcaShop.E2eTests --filter
  "FullyQualifiedName~ProductPageE2eTest"` ran here with no `E2E_BASE_URL`: 1 passed, 0 skipped. Decision
  `CAT-02-01` (how the gate gets a running shop) no longer blocks this stage.
- **Browser-test break:** the break sends every catalogue card's "View Details" to the first product. The suite now
  starts the shop from the tree it is built in, so the break applies to a scratch copy like the others do.
  **Not verified here:** `git apply --check` of the five patches was not approved in this session, so the patches
  have not been checked by `git apply`. They were written by hand against the context lines read from the files,
  and the browser break was not run.
- **Shared-scenarios rule:** AGENTS.md requires every browser test's `DisplayName` to be a scenario title in
  `../dca-sample-specification/scenarios.md` (`SharedScenariosTest`, which runs only with `-p:SpecificationPath=…`).
  The specification was outside this stage's inputs and could not be read here. "View Details on a catalogue card
  opens that product's page" therefore still has to be added there as a scenario, and the Java suite needs a test
  under the same title. Otherwise `SharedScenariosTest` fails when the specification is supplied. The user owns
  semantics.
- Unit tests: none. The plan names no domain invariant, and the story adopts page presentation only.
- Spikes: none written. The shop started in the background for the browser run (`dotnet run --project
  src/DcaShop.Web`, port 5080) may still be running, because stopping it was not approved.
