# Plan — CAT-01: The catalogue lists the range

Adopt mode (`status: adopted`): the story describes behaviour the shop already has; nothing is built, no
production code and no existing test changes. Stage done in-session; the profile names no `carrier.plan`.
The knowledge skill `dca-knowledge` is named in the profile, but an adoption decides no pattern, so no
catalog node was consulted.

Decision CAT-01-01 answered option 2: the code was wrong about the page title. The Java shop titles the
catalogue "Product Catalog"; the .NET shop is brought in line by the separate change story `catalogue-title`,
which CAT-01 `depends_on`. CAT-01 stays adopted and is adopted once that story is delivered (see
`catalogue-page-heading` below).

## Context

`product` — the Product Catalog context (`src/DcaShop.Product`, marker `ProductContext.cs`). It is on the
designed map (`project/domain.md`, Bounded Contexts: `Product`, Core) and on the generated map
(`docs/architecture/context-map.md`: `Product | Product Catalog | Product management and catalog browsing`).
The catalogue page, its listing order and the seeded range are all Product's: the page is served by
`Adapter/Incoming/Web/ProductPageController.Catalog` over `IGetAllProductsInputPort`, and the seed is
Product's own start-up adapter `Adapter/Incoming/Bootstrap/SampleDataSeeder`. The story touches no
relationship between contexts (price and stock badges are out of scope), so no difference between the two
maps bears on it.

Actor and surface: the visitor reaches the catalogue through the server-rendered page `GET /products`
(`ProductPageController.cs:9,21`), one of the shopper surfaces in `project/product.md` ("Surfaces": home page,
catalogue, product page, …). No new surface.

## Changes

| Element | Kind | Location | New or changed |
| --- | --- | --- | --- |
| — | — | — | none (adopt mode) |

Where the behaviour lives:

| Element | Kind | Location | Role in the story |
| --- | --- | --- | --- |
| `SampleDataSeeder` | incoming adapter (start-up) | `src/DcaShop.Product/Adapter/Incoming/Bootstrap/SampleDataSeeder.cs:22-50,63-76` | seeds the 21 products through `ICreateProductInputPort` |
| `InMemoryProductRepository.FindAllAsync` | outgoing adapter (persistence) | `src/DcaShop.Product/Adapter/Outgoing/Persistence/InMemoryProductRepository.cs:52-53` | orders by `ProductName` with `StringComparer.Ordinal` |
| `GetAllProductsUseCase` | use case / `IGetAllProductsInputPort` | `src/DcaShop.Product/Application/GetAllProducts/GetAllProductsUseCase.cs:16-21` | keeps the repository's order, enriches each product |
| `ProductPageController.Catalog` | incoming web adapter | `src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs:21-29` | maps the result in order to `ProductCatalogPageViewModel` |
| `Catalog.cshtml` | Razor view | `src/DcaShop.Web/Views/Product/Catalog.cshtml` | title, breadcrumb, heading, one card per product |

## Acceptance criteria

- **catalogue-lists-the-seeded-range** (happy path): Given the shop has started and seeded the sample
  catalogue of `seed-catalogue.json`, When a visitor opens the catalogue page, Then it shows 21 product cards.
  → level: **e2e** — Playwright, `tests/DcaShop.E2eTests` (profile `e2eTest:`, `browser: playwright`).
  - Lives in: `SampleDataSeeder.cs:25-49` (21 entries: 8 Books, 4 Modeling, 3 Apparel, 4 Desk & Office, 2
    Stickers & Pins); `Catalog.cshtml:12-14` renders one `data-test="product-card"` per product.
  - Covered by: `tests/DcaShop.E2eTests/SeededCatalogE2eTest.cs`, `SeededCatalogE2eTest.CatalogListsTheSeededProductsInNameOrder`
    — `Assert.Equal(21, titles.Count)` (line 20). It counts `product-card-title` elements, one per card
    (`Catalog.cshtml:26` sits inside each card), so it observes the card count.

- **catalogue-is-in-name-order**: Given the seeded sample catalogue, When a visitor opens the catalogue page,
  Then the card titles are in ordinal order of the product name, And the first card is
  `"Bounded Context" Enamel Pin`.
  → level: **integration** — `tests/DcaShop.IntegrationTests` (`test.integration:`), `GET /products` through
  `WebApplicationFactory<Program>`.
  - Lives in: `InMemoryProductRepository.cs:53` (`OrderBy(p => p.Name.Value, StringComparer.Ordinal)`);
    `GetAllProductsUseCase.cs:18-20` and `ProductPageController.cs:25-27` keep that order; the seed name
    `"\"Bounded Context\" Enamel Pin"` at `SampleDataSeeder.cs:49` sorts first (`"` precedes letters ordinally;
    the other quoted names, `"Domain over Framework"` / `"Ports & Adapters"` / `"Ubiquitous Language"`, sort after it).
  - Covered by: `SeededCatalogE2eTest.CatalogListsTheSeededProductsInNameOrder` (lines 21-22, browser level)
    and `tests/DcaShop.UnitTests/Product/InMemoryProductRepositoryTest.cs`,
    `InMemoryProductRepositoryTest.FindAllIsOrderedByProductNameWhateverTheOrderTheyWereSavedIn` (unit level).
    **No integration test** observes the order through the page.

- **card-shows-name-description-and-image**: Given the seeded product "Domain-Driven Design", When a visitor
  opens the catalogue page, Then its card shows the name "Domain-Driven Design", its description and its image.
  → level: **integration** — `tests/DcaShop.IntegrationTests`, `GET /products`.
  - Lives in: `Catalog.cshtml:16-18` (`<img src="@product.ImageUrl" alt="@product.Name">`), `:26` (name in
    `data-test="product-card-title"`), `:27-29` (description in `product-card__description`); data from
    `SampleDataSeeder.cs:25` (image `/images/products/ddd-book.webp`).
  - Covered by: **none**. `tests/DcaShop.IntegrationTests/ProductPageTest.cs`,
    `TheProductPageShowsTheImageTheDescriptionAndTheCategory` asserts the same image and description on the
    *product page* (CAT-02's scope), not on the catalogue card.

- **card-leads-to-the-product**: Given the seeded sample catalogue, When a visitor opens the catalogue page,
  Then every card offers a "View Details" link to that product's page.
  → level: **integration** — `tests/DcaShop.IntegrationTests`, `GET /products` and following the link.
  - Lives in: `Catalog.cshtml:41` (`<a … href="/products/@product.ProductId" data-test="view-product">View Details</a>`
    inside every card); target `ProductPageController.Detail` (`ProductPageController.cs:31-41`).
  - Covered by: partly. `ProductPageTest.ProductPathAsync` (`ProductPageTest.cs:89-96`) and
    `tests/DcaShop.E2eTests/ProductPageE2eTest.cs`, `ViewDetailsOnACatalogueCardOpensThatProductsPage` follow the
    link of **one** card. **No test** asserts the link text "View Details" or that **every** card carries a
    link to its own product.

- **catalogue-page-heading**: Given the seeded sample catalogue, When a visitor opens the catalogue page, Then
  the page title is "Product Catalog", the heading reads "Our Products" and the breadcrumb reads
  "Home / Products".
  → level: **integration** — `tests/DcaShop.IntegrationTests`, `GET /products`.
  - Lives in: `Catalog.cshtml:2` (`ViewData["Title"] = "Product Catalog"`, rendered verbatim by
    `Views/Shared/_Layout.cshtml`'s `<title>` per CAT-01-01's evidence), `:3-7` (breadcrumb Home / Products),
    `:8` (`<h1>Our Products</h1>`). **The title line is the uncommitted work of `catalogue-title`** (working
    tree; `git diff` shows `"Products"` → `"Product Catalog"`); that story is not delivered yet and waits on
    decision `catalogue-title-01`. CAT-01 is adoptable only once it is.
  - Covered by: `tests/DcaShop.E2eTests/CatalogTitleE2eTest.cs`, `CatalogTitleE2eTest.CatalogPageIsTitledProductCatalog`
    (catalogue-title's test, browser level: title, heading, breadcrumb); heading also by
    `ProductPageTest.TheBreadcrumbLeadsFromHomeOverProductsToTheProduct` (line 70) and
    `ProductPageTest.BackToProductsReturnsToTheCatalogue` (line 85). **No integration test** asserts the
    catalogue's title or its breadcrumb.

Gaps for the test stage: `catalogue-is-in-name-order`, `card-shows-name-description-and-image`,
`card-leads-to-the-product` and `catalogue-page-heading` have no test at their planned level. Adopt mode
changes no existing test; a covering test is new, in `tests/DcaShop.IntegrationTests`.

## Files

- `src/DcaShop.Web/Views/Product/Catalog.cshtml` — read: the markup and `data-test` hooks every criterion observes.
- `src/DcaShop.Product/Adapter/Incoming/Bootstrap/SampleDataSeeder.cs` — read: the 21 seeded products, names, descriptions, images.
- `src/DcaShop.Product/Adapter/Outgoing/Persistence/InMemoryProductRepository.cs` — read: the ordinal name order (line 53).
- `src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs` — read: route `/products` and the product-page target.
- `tests/DcaShop.IntegrationTests/ProductPageTest.cs` — read: the pattern to mirror (HTTP-level catalogue parsing:
  `CardTitle`, `CardLink`, `Section`, `Text` helpers against `WebApplicationFactory<Program>`).
- `tests/DcaShop.E2eTests/SeededCatalogE2eTest.cs` — read: the happy-path coverage.
- `tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs` — read: page object and selectors (`product-card`,
  `product-card-title`, `view-product`, `breadcrumb`).
- `tests/DcaShop.E2eTests/CatalogTitleE2eTest.cs` — read: catalogue-title's coverage of the title/heading/breadcrumb.

## Glossary proposals

- Catalogue page: the shopper page at `/products` that lists every product of the catalogue as a card, in
  ordinal order of the product name. (The Product glossary names `Category` as "used to structure the catalog"
  but has no entry for the page or its order.)
- Product card: one product's entry on the catalogue page — name, description, image, and a "View Details"
  link to the product page.

## Open assumptions

- `seed-catalogue.json` is a vector of the unpublished specification (`../dca-sample-specification/`), not a file
  in this repository (grep: no hit outside the backlog). The plan assumes it holds the same 21 products the
  seeder carries inline (`SampleDataSeeder.cs:22-50`); the shared scenario `scenario.catalog.seeded-in-name-order`
  (story Notes) is the one `SeededCatalogE2eTest` implements under the title "The catalog lists the seeded
  products in name order".
- `catalogue-page-heading` rests on `catalogue-title` being delivered with the title "Product Catalog"; whatever
  `catalogue-title-01` decides about the heading's `data-test`, the heading's text stays "Our Products".
- `tests/DcaShop.IntegrationTests/ProductPageTest.cs` and `tests/DcaShop.E2eTests/ProductPageE2eTest.cs` are
  uncommitted (working tree) and belong to CAT-02's scope; they are cited as partial coverage, not claimed by CAT-01.
