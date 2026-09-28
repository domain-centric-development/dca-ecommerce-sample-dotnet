# Plan — CAT-05: Every page shares the shop's header and footer

Adopt mode (`status: adopted`): the story describes behaviour the shop already has. Nothing is built, and no
production code or existing test changes. The stage was done in-session: the profile names no `carrier.plan`.
The profile names `dca-knowledge` as its knowledge skill, but an adoption decides no pattern, so no catalog node
was consulted.

## Context

`portal` is the Portal context (`src/DcaShop.Portal`, marker `PortalContext.cs`). Both maps have it:

- designed map: `project/domain.md:27`, `Portal` — "Web portal, UI composition, cross-context views", Generic (UI),
  Separate Ways to every context (`project/domain.md:71`: "Composition happens in the UI; no cross-context
  references").
- generated map: `docs/architecture/context-map.md:25`, `Portal | Portal | Web portal, user interface composition,
  and cross-context views | —`, a node without edges (`:38`).

The maps agree. The Portal glossary (`src/DcaShop.Portal/Domain/glossary.md:5`, `:17`) claims the landing page and
the navigation to the other contexts for Portal. The story touches no relationship between contexts: the header and
footer are plain anchors (`/`, `/products`, `/backoffice/events`) with no C# reference.

Finding (not settled here): the header and footer are rendered by the shared layout in the **web host**,
`src/DcaShop.Web/Views/Shared/_Layout.cshtml`, not by a type in `src/DcaShop.Portal`. That matches the repository's
convention that views live in the host (`AGENTS.md`, "Views mirror the Java sample's Pug templates"); Portal owns
the language (navigation), the host owns the markup. No change is planned for it.

Actor and surface: the visitor reaches the header and footer on every server-rendered page, because
`src/DcaShop.Web/Views/_ViewStart.cshtml:2` sets `Layout = "~/Views/Shared/_Layout.cshtml"` for all views and no
view overrides it (no other `Layout =` under `src/DcaShop.Web/Views`). The pages the scenarios name exist:
`GET /` (`src/DcaShop.Portal/Adapter/Incoming/Web/HomePageController.cs:8-9`), `GET /products` and
`GET /products/{productId}` (`src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs:9,21,31`). The
product "Domain-Driven Design" is seeded (`src/DcaShop.Product/Adapter/Incoming/Bootstrap/SampleDataSeeder.cs:25`,
`BOOK-001`). The product description lists these surfaces (`project/product.md`, `## Surfaces`). No new surface.

## Changes

| Element | Kind | Location | New or changed |
| --- | --- | --- | --- |
| — | — | — | none (adopt mode) |

Where the behaviour lives:

| Element | Kind | Location | Role in the story |
| --- | --- | --- | --- |
| `_Layout.cshtml` header | shared layout | `src/DcaShop.Web/Views/Shared/_Layout.cshtml:21-51` | `<header data-test="site-header">` on every page |
| logo | anchor | `_Layout.cshtml:23` | `<a href="/" data-test="site-logo">` with wordmark `domaincentric<span class="brand-tld">.commerce</span>` — text "domaincentric.commerce" |
| header navigation | anchors | `_Layout.cshtml:24-29` (`:26` `nav-home-link` "Home" → `/`, `:27` `nav-products-link` "Products" → `/products`) | the two navigation targets |
| `_Layout.cshtml` footer | shared layout | `_Layout.cshtml:65-80` | `<footer data-test="site-footer">` |
| footer text | text | `_Layout.cshtml:67` | "domaincentric.commerce &mdash; Built with Domain-Centric Architecture" |
| footer link | anchor | `_Layout.cshtml:68` | `<a href="/backoffice/events" data-test="footer-event-log-link">Event Log</a>` |
| `_ViewStart.cshtml` | view start | `src/DcaShop.Web/Views/_ViewStart.cshtml:2` | applies the layout to every view |
| `HomePageController.Index` | incoming web adapter | `src/DcaShop.Portal/Adapter/Incoming/Web/HomePageController.cs:8-9` | the home page `GET /` |
| `ProductPageController.Catalog` / `.Detail` | incoming web adapter | `src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs:21-40` | the catalogue and the product page |

## Acceptance criteria

- logo-leads-home: Given a visitor on the product page of "Domain-Driven Design", when they follow the logo
  "domaincentric.commerce" in the header, then the home page opens.  →  level: e2e — Playwright
  (`tests/DcaShop.E2eTests`), **happy path**.
  Lives in: `_Layout.cshtml:23` (`site-logo`, `href="/"`); home page `HomePageController.cs:8-9`.
  Covered by: none. No test in `tests/` refers to `site-logo`, `site-header` or the wordmark.
- nav-home-opens-the-home-page: Given a visitor on the catalogue page, when they follow "Home" in the header
  navigation, then the home page opens.  →  level: integration — `tests/DcaShop.IntegrationTests`
  (`WebApplicationFactory<Program>`, HTTP surface).
  Lives in: `_Layout.cshtml:26` (`nav-home-link`, "Home", `href="/"`).
  Covered by: none. No test refers to `nav-home-link`.
- nav-products-opens-the-catalogue: Given a visitor on the home page, when they follow "Products" in the header
  navigation, then the catalogue page "Our Products" opens.  →  level: integration — `tests/DcaShop.IntegrationTests`.
  Lives in: `_Layout.cshtml:27` (`nav-products-link`, "Products", `href="/products"`); the catalogue heading
  "Our Products" in `src/DcaShop.Web/Views/Product/Catalog.cshtml`.
  Covered by: none for the navigation link. The heading alone is asserted by
  `tests/DcaShop.IntegrationTests/CatalogueTest.cs:86` (`Assert.Equal("Our Products", …)`); the home page's other
  links to the catalogue are covered by `HomePageTest.BrowseProductsOpensTheCatalogue` and
  `HomePageTest.ShopNowBelowTheCallToShopOpensTheCatalogue` — neither follows the header link.
- footer-names-the-shop: Given the shop has started, when a visitor opens the catalogue page, then the footer reads
  "domaincentric.commerce — Built with Domain-Centric Architecture", and it offers an "Event Log" link to
  `/backoffice/events`.  →  level: integration — `tests/DcaShop.IntegrationTests`.
  Lives in: `_Layout.cshtml:67` (text; `&mdash;` renders as "—"), `_Layout.cshtml:68` (`footer-event-log-link`).
  Covered by: none. No test refers to `site-footer`, `footer-event-log-link` or "Built with".
- header-and-footer-on-the-home-page: Given the shop has started, when a visitor opens the home page, then it shows
  the same header, with "Home" and "Products", and the same footer as the catalogue page.  →  level: integration —
  `tests/DcaShop.IntegrationTests`.
  Lives in: `_ViewStart.cshtml:2` (one layout for all views) and `_Layout.cshtml:21-80`.
  Covered by: none.

Detail criteria the story names (each part of the scenario above it, listed so no stage drops it):
- the logo's text is "domaincentric.commerce" (`logo-leads-home`);
- the navigation link texts are exactly "Home" and "Products" (`nav-home-opens-the-home-page`,
  `nav-products-opens-the-catalogue`, `header-and-footer-on-the-home-page`);
- the footer text is exactly "domaincentric.commerce — Built with Domain-Centric Architecture", with an em dash
  (`footer-names-the-shop`);
- the "Event Log" link targets `/backoffice/events` (`footer-names-the-shop`).

## Files

- `src/DcaShop.Web/Views/Shared/_Layout.cshtml` — read: where header, logo, navigation and footer live.
- `src/DcaShop.Web/Views/_ViewStart.cshtml` — read: why every page carries the layout.
- `src/DcaShop.Portal/Adapter/Incoming/Web/HomePageController.cs` — read: the home page route.
- `src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs` — read: catalogue and product page routes.
- `src/DcaShop.Product/Adapter/Incoming/Bootstrap/SampleDataSeeder.cs` — read: "Domain-Driven Design" is seeded.
- `tests/DcaShop.IntegrationTests/HomePageTest.cs` — read: the pattern to mirror (link lookup by `data-test`,
  follow its target, assert the `<h1>`); the `Link`/`Text` helpers.
- `tests/DcaShop.IntegrationTests/CatalogueTest.cs` — read: the catalogue heading assertion.
- `tests/DcaShop.E2eTests/HomePageE2eTest.cs`, `tests/DcaShop.E2eTests/Pages/HomePage.cs`,
  `tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs` — read: the page objects to reuse for the happy path.

## Open assumptions

- The story's "Then the home page opens" is observable as the response of `GET /` (the home page view); the plan
  does not require a particular element on it beyond what CAT-04 already names.
- The shared-scenario check (`SharedScenariosTest`, `AGENTS.md` "Shared semantics since WP-39") requires every
  browser test title to be a scenario in the specification's `scenarios.md`. Whether `logo-leads-home` already has a
  scenario there was not read (outside this stage's inputs); the test stage has to check it.
- The theme switcher inside the footer (`_Layout.cshtml:69-79`) and the header actions (`:30-50`) are out of scope
  and are not asserted.
