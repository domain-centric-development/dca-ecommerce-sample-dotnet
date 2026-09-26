# Plan — CAT-02: The product page

Mode: **adopt** (`status: adopted`). The story describes behaviour the shop already has, so nothing is built.
This plan names, for each scenario, where the behaviour lives and which existing test covers it. It plans
no change to production code or to an existing test.

Carrier: done in this session. The profile names no `carrier.plan`. The profile names `knowledge: dca-knowledge`,
but it was not consulted because adopting existing behaviour decides no pattern question.

## Context

**Product** (Product Catalog, Core). The designed map (`project/domain.md`, table "Bounded Contexts") gives
it "master data, categories", and the generated map (`docs/architecture/context-map.md`, row `Product`) gives it
"Product management and catalog browsing". Name, description, category and image are the Product aggregate's
descriptive master data (`src/DcaShop.Product/Domain/glossary.md`, "Product"). The page is served by the
context's own incoming web adapter, `ProductPageController`. The price and the availability on the same page
belong to Pricing and Inventory, and the story lists them as out of scope (`PRC-01`, `AVL-01`).

The designed and generated maps agree on Product and its relationships (Pricing → Product and Inventory →
Product, ACL / Api, Partnership, on both). The identity dependency Account → Product is missing from the
generated map, as `project/domain.md` says it will be. That is not a finding for this story.

The surface already exists and `project/product.md` names it under `## Surfaces`: "catalogue, product page".
`GET /products/{productId:guid}` (`ProductPageController.cs:31`) serves the product page to every visitor
without authorisation. The catalogue card links to it (`Catalog.cshtml:41`).

## Changes

| Element | Kind | Location | New or changed |
| --- | --- | --- | --- |
| — | — | — | none (adopt mode) |

For reference, these existing elements carry the behaviour:

| Element | Kind | Location |
| --- | --- | --- |
| `ProductPageController.Detail` | incoming web adapter | `src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs:31-44` |
| `ProductDetailPageViewModel` | page view model | `src/DcaShop.Product/Adapter/Incoming/Web/ProductDetailPageViewModel.cs` |
| `IGetProductByIdInputPort` / `GetProductByIdUseCase` | input port / use case (query) | `src/DcaShop.Product/Application/GetProductById/GetProductByIdUseCase.cs:17-27` |
| `EnrichedProduct` | read model | `src/DcaShop.Product/Domain/Model` (glossary "EnrichedProduct") |
| `Detail.cshtml` | Razor view | `src/DcaShop.Web/Views/Product/Detail.cshtml` |
| `Catalog.cshtml` | Razor view (card link) | `src/DcaShop.Web/Views/Product/Catalog.cshtml:41` |
| `SampleDataSeeder` | seed data | `src/DcaShop.Product/Adapter/Incoming/Bootstrap/SampleDataSeeder.cs:25,26,32` |

## Acceptance criteria

- view-details-opens-the-product-page (happy path): given the seeded product "Domain-Driven Design", a
  visitor follows the "View Details" link on its catalogue card. The product page of "Domain-Driven Design"
  opens and its heading reads "Domain-Driven Design".
  → level: **e2e**, Playwright (`tests/DcaShop.E2eTests`, `e2eTest:` + `browser: playwright`).
  - Lives in: `Catalog.cshtml:41` renders `<a … href="/products/@product.ProductId" data-test="view-product">View Details</a>`.
    `ProductPageController.cs:31-41` answers `GET /products/{guid}` through `IGetProductByIdInputPort`.
    `Detail.cshtml:12` renders `<h1 class="product-detail__title" data-test="product-detail-title">@Model.Name</h1>`.
    Seed: `SampleDataSeeder.cs:25` ("BOOK-001", "Domain-Driven Design").
  - Covered by: **none** for this product. `ProductCatalogPage.ViewFirstProductAsync` / `ViewProductAsync(int)`
    (`tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs:30-40`) click `view-product` by position.
    `MobileLayoutE2eTest.ShopFitsAPhoneViewport` and the checkout suites use it, but none of them names
    "Domain-Driven Design" or checks the heading. `HomeSliderE2eTest` "A slider card leads to its product page"
    (`HomeSliderE2eTest.cs:78-90`) checks the heading (`ProductDetailPage.TitleAsync`), but it arrives from the
    homepage slider, not from the catalogue card.

- product-page-shows-image-description-and-category: given the seeded product "Domain-Driven Design", a visitor
  opens its product page. The page shows the product's image and the description "The seminal work by Eric
  Evans that introduced the software industry to Domain-Driven Design. This essential guide teaches you how to
  tackle complexity in the heart of software by connecting implementation to an evolving model of the business
  domain.", and it shows "Category" with the value "Books".
  → level: **integration**, HTTP surface through `WebApplicationFactory` (`tests/DcaShop.IntegrationTests`,
  `test.integration:`).
  - Lives in: `Detail.cshtml:23-27` renders `<img src="@Model.ImageUrl" alt="@Model.Name">`. `Detail.cshtml:35-38`
    renders the description in `.product-detail__description`. `Detail.cshtml:40` renders the meta label
    "Category" with `@Model.Category`. The view model is filled at `ProductPageController.cs:43-44`. Seed:
    `SampleDataSeeder.cs:25` (image `/images/products/ddd-book.webp`, category "Books", the description verbatim).
  - Covered by: the **image only, and only partly**: `HomeSliderE2eTest` "A slider card shows the product's
    image, name and price" (`HomeSliderE2eTest.cs:61-75`) asserts that the page's image equals the card's
    (`ProductDetailPage.ImageSourceAsync`). Description and category: **none**.

- product-page-title-is-the-product-name: given the seeded product "Clean Architecture", a visitor opens its
  product page. The browser tab title is "Clean Architecture".
  → level: **integration**, HTTP surface (the `<title>` element is in the rendered HTML).
  - Lives in: `Detail.cshtml:2` sets `ViewData["Title"] = Model.Name`. The layout renders
    `<title>@(ViewData["Title"] ?? "domaincentric.commerce")</title>` (`src/DcaShop.Web/Views/Shared/_Layout.cshtml:9`).
    Seed: `SampleDataSeeder.cs:26`.
  - Covered by: **none**.

- product-page-breadcrumb: given the seeded product "Clean Architecture", a visitor opens its product page.
  The breadcrumb reads "Home / Products / Clean Architecture", "Home" links to the home page and "Products"
  to the catalogue page.
  → level: **integration**, HTTP surface.
  - Lives in: `Detail.cshtml:3-9`, `data-test="breadcrumb"`: `<a href="/">Home</a>`, separator `/`,
    `<a href="/products">Products</a>`, separator `/`, `<span class="breadcrumb__current">@Model.Name</span>`.
  - Covered by: **none**.

- back-to-products-returns-to-the-catalogue: given a visitor on the product page of "Team Topologies", they
  follow "Back to Products". The catalogue page "Our Products" opens.
  → level: **integration**, HTTP surface: the link's target, then that page's `<h1>`.
  - Lives in: `Detail.cshtml:64` renders `<a class="btn btn--ghost" href="/products" data-test="product-back-link">Back to Products</a>`.
    `Catalog.cshtml:8` renders `<h1>Our Products</h1>`. Seed: `SampleDataSeeder.cs:32`.
  - Covered by: **none**. The page object has `ProductDetailPage.BackToCatalogAsync`
    (`tests/DcaShop.E2eTests/Pages/ProductDetailPage.cs:33-37`), but no test calls it.

The story has no further concrete details (wording, placement, ordering) beyond those restated in the
scenarios above.

## Files

- `src/DcaShop.Web/Views/Product/Detail.cshtml` — read: the product page markup (breadcrumb, heading, image,
  description, category, back link, title)
- `src/DcaShop.Web/Views/Product/Catalog.cshtml` — read: the "View Details" card link (`data-test="view-product"`)
  and the "Our Products" heading
- `src/DcaShop.Web/Views/Shared/_Layout.cshtml` — read: the `<title>` element (line 9)
- `src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs` — read: the routes `/products` and
  `/products/{productId:guid}`
- `src/DcaShop.Product/Adapter/Incoming/Web/ProductDetailPageViewModel.cs` — read: what the view receives
- `src/DcaShop.Product/Application/GetProductById/GetProductByIdUseCase.cs` — read: the query behind the page
- `src/DcaShop.Product/Adapter/Incoming/Bootstrap/SampleDataSeeder.cs` — read: the seeded products named by the
  scenarios (lines 25, 26, 32)
- `tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs` — read: the catalogue page object (`view-product`,
  `product-card-title`). Opening a card by product name is not offered yet
- `tests/DcaShop.E2eTests/Pages/ProductDetailPage.cs` — read: the product page object (`TitleAsync`,
  `ImageSourceAsync`, `BackToCatalogAsync`)
- `tests/DcaShop.E2eTests/HomeSliderE2eTest.cs` — read: the nearest existing checks of the page's heading and
  image (lines 61-90)
- `tests/DcaShop.IntegrationTests/ShopFlowTest.cs` — read: the pattern of an HTTP-level page test (catalogue →
  `GET /products/{id}`, lines 22-36)

## Open assumptions

- Adopting describes the behaviour; the plan adds no test. Four scenarios have no covering test and one is
  covered only partly (see above). Writing tests for them belongs to the test stage, if the pipeline asks for
  them for an adopted story. The plan names the levels they would take.
- "Home links to the home page" is checked only as the link target `/`. What the home page shows is `CAT-04`,
  which is out of scope.
- The inventory reference in the story's notes (P6, P7) points to an inventory document that was not among
  this stage's inputs. It was not read.
- The shared specification (`../dca-sample-specification/scenarios.md`) was not among this stage's inputs.
  Whether the happy path's title has to be a scenario there, for `SharedScenariosTest`, is for the test stage.
