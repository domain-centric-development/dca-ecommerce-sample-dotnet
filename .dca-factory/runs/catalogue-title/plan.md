# Plan — catalogue-title: The catalogue page is titled "Product Catalog"

## Context

Bounded context **Product** (Product Catalog, core subdomain). The story's `context: product` is on the designed map
(`project/domain.md`, "Bounded Contexts": `Product` — master data, categories) and on the generated map
(`docs/architecture/context-map.md`, "Bounded Contexts": module `Product`, name "Product Catalog"). The two maps agree
on the context; the story touches no relationship.

The catalogue page is Product's incoming web adapter: `ProductPageController` (`[Route("products")]`, `[HttpGet("")]`,
`src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs:9,21`) renders
`~/Views/Product/Catalog.cshtml` (line 28). The view sets `ViewData["Title"] = "Products"`
(`src/DcaShop.Web/Views/Product/Catalog.cshtml:2`), and the shared layout writes that value unchanged into the
document title: `<title>@(ViewData["Title"] ?? "domaincentric.commerce")</title>`
(`src/DcaShop.Web/Views/Shared/_Layout.cshtml:9`) — no suffix, so the tab reads exactly the view's value.

The actor (a visitor) already reaches the behaviour: the catalogue page `/products` is a listed surface
(`project/product.md`, "Surfaces": "catalogue"). No new surface is needed.

The change is presentation only: no domain type, use case, port or adapter logic changes, so no architecture
pattern question arises and `dca-knowledge` (named in the profile) was not consulted — there was nothing for it to
decide. `carrier.plan` is not named in the profile; the stage was done in-session.

## Changes

| Element | Kind | Location | New or changed |
| --- | --- | --- | --- |
| Catalogue page view (`Catalog.cshtml`) — document title | Razor view of Product's incoming web adapter (lives in the web host, `Views/Product/`) | `src/DcaShop.Web/Views/Product/Catalog.cshtml` | changed: `ViewData["Title"]` from `"Products"` to `"Product Catalog"` |
| `ProductCatalogPage` page object — read the document title | E2E page object | `tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs` | changed (test support; a later stage's concern) |

Unchanged on purpose (story assumption, answered by CAT-01-01): the heading `<h1>Our Products</h1>`
(`Catalog.cshtml:8`) and the breadcrumb "Home / Products" (`Catalog.cshtml:3-7`).

## Acceptance criteria

- catalogue-tab-reads-product-catalog (happy path): Given the seeded sample catalogue, when a visitor opens the
  catalogue page, then the page title is "Product Catalog"  →  level: e2e — Playwright (`tests/DcaShop.E2eTests`,
  profile `e2eTest:` / `browser: playwright`), happy path
- catalogue-tab-reads-product-catalog (detail, same test): the title is exactly "Product Catalog" — no prefix, no
  suffix, not "Products"; the heading still reads "Our Products" and the breadcrumb's current segment still reads
  "Products" (story assumption, CAT-01-01)  →  level: e2e — asserted in the same happy-path test

## Files

- `src/DcaShop.Web/Views/Product/Catalog.cshtml` — changes: line 2, `ViewData["Title"] = "Product Catalog"`
- `tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs` — changes: a way to read the document title (e.g. via
  Playwright's `IPage.TitleAsync()`); the file already carries uncommitted changes from another story — add to it,
  do not revert them
- `tests/DcaShop.E2eTests/SeededCatalogE2eTest.cs` — read: the pattern for a seeded-catalogue browser test
  (`BaseE2eTest`, `[E2eFact(DisplayName = …)]`, `ProductCatalogPage.NavigateToAsync`); the new test may live here
  or beside it
- `src/DcaShop.Web/Views/Shared/_Layout.cshtml` — read: line 9, how `ViewData["Title"]` becomes `<title>`
- `src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs` — read: route `/products` and the view it
  renders
- `tests/DcaShop.IntegrationTests/ProductPageTest.cs` — read: lines 69-70 assert the breadcrumb link "Products" and
  the heading "Our Products" of the catalogue page; they stay green and must not change

## Open assumptions

- answered (story, CAT-01-01): the heading "Our Products" and the breadcrumb "Home / Products" stay as they are.
- The story's `## Changed expectations` line ("reads 'Products' now and 'Product Catalog' after") is backed by no
  existing test: no test in `tests/` asserts the catalogue page's document title (searched for `<title`,
  `TitleAsync`, `"Products"`, `Product Catalog`; the only `<title>` assertion is on the product detail page,
  `ProductPageTest.cs:54`). Hence no `## Changed tests` section.
- The story's motive is that the tab reads the same "in both shops". The Java sample's template and the
  specification's `scenarios.md` are outside this stage's inputs and were not read. The E2E test's `DisplayName` has
  to be a scenario title in `../dca-sample-specification/scenarios.md` (`SharedScenariosTest`, AGENTS.md "Shared
  semantics since WP-39"); if the scenario is not there yet, it has to be added there and in the Java suite — sync
  duty, not part of this story's change list.
