# Tests — CAT-03

Adopt mode (`status: adopted`). Carrier: in-session; `e2e-testing` (`carrier.test`) was not needed, because no
browser test was written. Knowledge skill `dca-knowledge`: not consulted, because adoption raises no pattern question.

<!-- gate:tests -->
| criterion | test |
| --- | --- |
| unknown-product-shows-the-not-found-page | DcaShop.E2eTests.NotFoundTitleE2eTest#NotFoundPageIsTitledWithTheShopName |
| not-found-page-has-the-shop-title | DcaShop.E2eTests.NotFoundTitleE2eTest#NotFoundPageIsTitledWithTheShopName |
| browse-all-products-leads-to-the-catalogue | DcaShop.IntegrationTests.NotFoundPageTest#BrowseAllProductsOnTheNotFoundPageOpensTheCatalogue |
| go-to-homepage-leads-to-the-home-page | DcaShop.IntegrationTests.NotFoundPageTest#GoToHomepageOnTheNotFoundPageOpensTheHomePage |

## Characterization
- DcaShop.IntegrationTests.NotFoundPageTest#BrowseAllProductsOnTheNotFoundPageOpensTheCatalogue
- DcaShop.IntegrationTests.NotFoundPageTest#GoToHomepageOnTheNotFoundPageOpensTheHomePage

## Files
- tests/DcaShop.IntegrationTests/NotFoundPageTest.cs
- tasks/CAT-03/breaks/DcaShop.IntegrationTests.NotFoundPageTest--BrowseAllProductsOnTheNotFoundPageOpensTheCatalogue.patch
- tasks/CAT-03/breaks/DcaShop.IntegrationTests.NotFoundPageTest--GoToHomepageOnTheNotFoundPageOpensTheHomePage.patch

## Notes
- Existing test, from the plan: `NotFoundTitleE2eTest#NotFoundPageIsTitledWithTheShopName` opens
  `/products/no-such-product`. It asserts "404", "Page Not Found" and the full message (whitespace collapsed), and the
  tab title "domaincentric.commerce". It belongs to `not-found-title` and is untracked, like the 404 view without a
  title that it depends on. CAT-03 holds only once `not-found-title` is committed (the plan's open assumption).
  It was not run in this stage, because it needs the browser suite.
- The plan put `not-found-page-has-the-shop-title` at integration level, but no integration test exists. In adopt mode
  the row maps to the existing browser test the plan names. The adopt gate checks no levels.
- Characterization tests: both are green on today's working tree. Each one opens `/products/no-such-product`,
  asserts `404`, reads the link by its `data-test` and checks its label. It then follows the `href` and asserts the
  target's `<h1>`: "Our Products" and "Welcome to domaincentric.commerce".
- Breaks: each patch changes one link's `href` in `src/DcaShop.Web/Views/Error/404.cshtml` and applies to the
  working tree, including the uncommitted `not-found-title` change. Both were verified by hand: with the browse link on `/`, only
  `BrowseAllProductsOnTheNotFoundPageOpensTheCatalogue` fails (expected "Our Products", actual "Welcome to
  domaincentric.commerce"). With the home link on `/products`, only `GoToHomepageOnTheNotFoundPageOpensTheHomePage`
  fails (expected "Welcome to domaincentric.commerce", actual "Our Products"). The view was then restored.
- unit tests: none — the plan names no invariant; nothing in the domain is involved.
- `dotnet format` was run on the new test file.
