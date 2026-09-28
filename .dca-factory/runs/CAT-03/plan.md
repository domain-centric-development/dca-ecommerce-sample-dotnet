# Plan — CAT-03: A product that does not exist

Adopt mode (`status: adopted`). No production code and no existing test changes are planned.

This is a second plan. The first one stopped on decision CAT-03-01. **Decision CAT-03-01 answered option 2 (correct
the shop):** the Java shop sets no title on its not-found page, so the .NET shop is brought in line by the change
story `not-found-title`, which CAT-03 depends on. CAT-03 stays adopted as written. That change is now in the
working tree: `src/DcaShop.Web/Views/Error/404.cshtml` no longer sets `ViewData["Title"]` (`git diff` removes the
old line 1, `@{ ViewData["Title"] = "Page Not Found"; }`), and `tasks/not-found-title/judge.md:4` reads
`verdict: pass`. It is **not committed yet**: the status line says "not-found-title is running". Everything below
describes the code with that change in place. See `## Open assumptions`.

Carrier: in-session. The profile names no `carrier.plan`. Knowledge skill `dca-knowledge` (named in the profile):
not consulted, because adopt mode raises no pattern question.

## Context

The story's context is `product` (Product Catalog). It is on the designed map (`project/domain.md:21`, `Product`,
Core) and on the generated map (`docs/architecture/context-map.md:27`, "Product Catalog"). The two maps agree.

The behaviour itself is not in the Product context. It lives in the web host, which belongs to no context (AGENTS.md,
"Stand-ins still in place"). This is a finding for the record, not a blocker for adopting:

- `/products/no-such-product` is not a GUID, so it does not match `ProductPageController.Detail`
  (`[HttpGet("{productId:guid}")]`, `src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs:31`). No
  route answers it, and the request becomes a 404.
- `app.UseStatusCodePagesWithReExecute("/error/{0}")` (`src/DcaShop.Web/Program.cs:86`) re-executes it as `/error/404`.
- `ErrorPageController.Show` (`src/DcaShop.Web/Controllers/ErrorPageController.cs:8-12`) renders
  `~/Views/Error/404.cshtml`.
- A GUID no product has reaches the same page: `Detail` returns `NotFound()` (`ProductPageController.cs:37`).

The project description covers this surface: `project/product.md:16` lists server-rendered pages for the shopper.
Nothing in `project/tech.md` is touched, because nothing is built.

## Changes

| Element | Kind | Location | New or changed |
| --- | --- | --- | --- |
| none | — | — | adopt mode: nothing is built |

## Acceptance criteria

- unknown-product-shows-the-not-found-page (happy path): Given the seeded sample catalogue, in which no product has
  the id "no-such-product", when a visitor opens the product page `/products/no-such-product`, then the page shows
  "404" and the heading "Page Not Found", and the message reads "The page you're looking for doesn't exist or has
  been removed. Perhaps you were looking for one of our products?"  →  level: e2e — Playwright,
  `tests/DcaShop.E2eTests`
  - Lives in: `src/DcaShop.Web/Views/Error/404.cshtml:2` (`error-page__code` "404"), `:3` (`h2.error-page__title`
    "Page Not Found"), `:4-5` (`error-page__message`, the text broken over two lines; the browser renders the line
    break as one space). Route: see Context.
  - Covered by: `tests/DcaShop.E2eTests/NotFoundTitleE2eTest.cs`, `NotFoundTitleE2eTest.NotFoundPageIsTitledWithTheShopName`
    (lines 17, 20-23). It opens `/products/no-such-product` and asserts "404", "Page Not Found" and the full message
    through `NotFoundPage.ShowsAsync`, which collapses whitespace (`Pages/NotFoundPage.cs:30-31`). This test is
    untracked and comes from `not-found-title`.
    `tests/DcaShop.IntegrationTests/ShopFlowTest.cs`, `UnknownPageRendersThe404Page` (lines 89-97), covers only
    `/does-not-exist`: the status code and `error-page__code`.
- not-found-page-has-the-shop-title: Given the seeded sample catalogue, when a visitor opens the product page
  `/products/no-such-product`, then the browser tab title is "domaincentric.commerce"  →  level: integration —
  `tests/DcaShop.IntegrationTests` (the `<title>` of the HTTP response)
  - Lives in: `src/DcaShop.Web/Views/Shared/_Layout.cshtml:9` (`<title>@(ViewData["Title"] ?? "domaincentric.commerce")</title>`).
    With `not-found-title` applied, `404.cshtml` sets no title, so the fallback applies (Decision CAT-03-01).
  - Covered by: at e2e level, `NotFoundTitleE2eTest.NotFoundPageIsTitledWithTheShopName` (line 19,
    `Assert.Equal("domaincentric.commerce", await notFound.DocumentTitleAsync())`). At integration level: none. The
    only title assertion on the shop name is for the home page (`tests/DcaShop.IntegrationTests/HomePageTest.cs:22-28`).
- browse-all-products-leads-to-the-catalogue: Given a visitor on the not-found page of `/products/no-such-product`,
  when they follow "Browse All Products", then the catalogue page "Our Products" opens  →  level: integration —
  `tests/DcaShop.IntegrationTests` (the link's `href` on the 404 response, then `GET` of that target shows
  `<h1>Our Products</h1>`)
  - Lives in: `src/DcaShop.Web/Views/Error/404.cshtml:7` (`href="/products"`, `data-test="error-browse-link"`,
    label "Browse All Products"). The target is `ProductPageController.Catalog` (`ProductPageController.cs:21-29`),
    and `src/DcaShop.Web/Views/Product/Catalog.cshtml:8` renders `<h1>Our Products</h1>`.
  - Covered by: in part. `NotFoundTitleE2eTest.NotFoundPageIsTitledWithTheShopName` line 24 asserts the label and
    `href="/products"`, but it does not follow the link. "Our Products" on `/products` is asserted elsewhere, but
    not reached from the 404 page: `tests/DcaShop.IntegrationTests/CatalogueTest.cs:86`, `SiteLayoutTest.cs:44`,
    `HomePageTest.cs:41`, `ProductPageTest.cs:70`. No test follows the link and checks the result.
- go-to-homepage-leads-to-the-home-page: Given a visitor on the not-found page of `/products/no-such-product`, when
  they follow "Go to Homepage", then the home page "Welcome to domaincentric.commerce" opens  →  level:
  integration — `tests/DcaShop.IntegrationTests` (the link's `href` on the 404 response, then `GET /` shows the
  hero heading, rendered as text)
  - Lives in: `src/DcaShop.Web/Views/Error/404.cshtml:8` (`href="/"`, `data-test="error-home-link"`, label
    "Go to Homepage"). `src/DcaShop.Web/Views/Home/Index.cshtml:3` renders
    `Welcome to domaincentric<span class="brand-tld">.commerce</span>`, which reads as text "Welcome to domaincentric.commerce".
  - Covered by: in part. `NotFoundTitleE2eTest.NotFoundPageIsTitledWithTheShopName` line 25 asserts the label and
    `href="/"` without following the link. The home heading is asserted from other starting points:
    `tests/DcaShop.IntegrationTests/SiteLayoutTest.cs:31`, `tests/DcaShop.E2eTests/HomePageE2eTest.cs:19`,
    `SiteHeaderE2eTest.cs:21`. No test follows the link from the 404 page.

## Files

- `src/DcaShop.Web/Views/Error/404.cshtml` — read: the page every scenario observes; since `not-found-title` it
  carries no title
- `src/DcaShop.Web/Views/Shared/_Layout.cshtml` — read: line 9, the tab title and its fallback
- `src/DcaShop.Web/Controllers/ErrorPageController.cs` — read: which view answers 404
- `src/DcaShop.Web/Program.cs` — read: line 86, status-code re-execution
- `src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs` — read: the `guid` route constraint and
  `NotFound()` for an unknown id
- `src/DcaShop.Web/Views/Product/Catalog.cshtml`, `src/DcaShop.Web/Views/Home/Index.cshtml` — read: the headings
  the two links lead to
- `tests/DcaShop.E2eTests/NotFoundTitleE2eTest.cs`, `tests/DcaShop.E2eTests/Pages/NotFoundPage.cs` — read: the
  existing browser test and page object for the not-found page (from `not-found-title`)
- `tests/DcaShop.IntegrationTests/ShopFlowTest.cs`, `tests/DcaShop.IntegrationTests/SiteLayoutTest.cs` — read: the
  existing 404 test and the pattern of following a link and reading the target's `<h1>`

## Open assumptions

- CAT-03 can be adopted only once `not-found-title` is committed. Until then the committed code still sets
  "Page Not Found" as the title, and `not-found-page-has-the-shop-title` holds only in the working tree.
- The coverage above names `NotFoundTitleE2eTest`, which belongs to `not-found-title` and is not committed yet.
- "no-such-product" stands for any address that names no product. Because of the GUID route constraint, this address
  never reaches the Product context. The not-found page is a web-host concern, even though the story says
  `context: product`.
- The story's `Out of scope` excludes the API (`/api/products/...`) and the MCP tools. Neither is planned.
