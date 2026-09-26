# Plan — CAT-04: The home page

Adopt mode (`status: adopted`): the story describes behaviour the shop already has. Nothing is built, and no
production code or existing test changes. The stage was done in-session: the profile names no `carrier.plan`.
The profile names `dca-knowledge` as its knowledge skill, but an adoption decides no pattern, so no catalog node
was consulted.

## Context

`portal` is the Portal context (`src/DcaShop.Portal`, marker `PortalContext.cs:15`,
`[BoundedContext("Portal", …)]`). Both maps have it:

- designed map: `project/domain.md:27`, `Portal` — "Web portal, UI composition, cross-context views", Generic (UI).
  It is Separate Ways to every context (`project/domain.md:71`: "Composition happens in the UI; no cross-context
  references").
- generated map: `docs/architecture/context-map.md:25`, `Portal | Portal | Web portal, user interface composition,
  and cross-context views | —`. It has a node and no edges (`:38`).

The two maps agree: Portal has no relationships. The Portal glossary (`src/DcaShop.Portal/Domain/glossary.md`,
"Own Terms: HomePage") says the context owns the landing page `GET /`. The story touches no relationship between
contexts. The home page only links to `/products` (Product) and `/cart` (Cart) as plain anchors, with no C#
reference.

Actor and surface: the visitor reaches the page through the server-rendered page `GET /`
(`HomePageController.cs:8-9`). The product description lists this surface (`project/product.md:16`: "Server-rendered
web pages for the shopper — home page, catalogue, …"). No new surface.

## Changes

| Element | Kind | Location | New or changed |
| --- | --- | --- | --- |
| — | — | — | none (adopt mode) |

Where the behaviour lives:

| Element | Kind | Location | Role in the story |
| --- | --- | --- | --- |
| `HomePageController.Index` | incoming web adapter | `src/DcaShop.Portal/Adapter/Incoming/Web/HomePageController.cs:8-9` | `GET /` renders `~/Views/Home/Index.cshtml` |
| `Index.cshtml` | Razor view | `src/DcaShop.Web/Views/Home/Index.cshtml` | title, hero, features, categories, call to action |
| `_Layout.cshtml` | shared layout | `src/DcaShop.Web/Views/Shared/_Layout.cshtml:9` | `<title>@(ViewData["Title"] ?? "domaincentric.commerce")</title>` adds no suffix |
| `Catalog.cshtml` | Razor view (Product) | `src/DcaShop.Web/Views/Product/Catalog.cshtml:8` | the target of both catalogue links: `<h1>Our Products</h1>` |
| Program wiring | web host | `src/DcaShop.Web/Program.cs:18` | `AddApplicationPart(typeof(DcaShop.Portal.PortalContext).Assembly)` exposes the controller |

## Acceptance criteria

- **home-page-welcomes-the-visitor** (happy path): Given the shop has started, When a visitor opens the home page
  `/`, Then the heading reads "Welcome to domaincentric.commerce", And the subtitle reads "Books, modelling supplies
  and hexagon merchandise for people who draw boundaries", And below it reads "Everything this architecture is made
  of: the books it grew out of, the supplies a design workshop runs on, and hexagons for your desk."
  → level: **e2e**: Playwright, `tests/DcaShop.E2eTests` (profile `e2eTest:`, `browser: playwright`).
  - Lives in: `Index.cshtml:2-5`: `section.hero[data-test="hero"]`, `h1.hero__title` (line 3,
    `Welcome to domaincentric<span class="brand-tld">.commerce</span>`, so the rendered text is the whole
    sentence), `p.hero__subtitle` (line 4), `p.hero__description` (line 5). The texts match the story word for word.
  - Covered by: **none**. `HomePage.NavigateToAsync` (`tests/DcaShop.E2eTests/Pages/HomePage.cs:43-49`) waits only
    for `data-test="hero"`. No test reads the heading, subtitle or description.

- **home-page-title**: Given the shop has started, When a visitor opens the home page, Then the browser tab title is
  "domaincentric.commerce".
  → level: **integration**: `tests/DcaShop.IntegrationTests` (`test.integration:`), `GET /` through
  `WebApplicationFactory<Program>`, reading `<title>`.
  - Lives in: `Index.cshtml:1` (`ViewData["Title"] = "domaincentric.commerce"`), rendered by `_Layout.cshtml:9`.
  - Covered by: **none**. `AccountFlowTest.cs:28,149` and `ApiFlowTest.cs:89` request `/` only for cookies and
    identity. They assert nothing about the page.

- **browse-products-opens-the-catalogue**: Given a visitor on the home page, When they follow "Browse Products", Then
  the catalogue page "Our Products" opens.
  → level: **integration**: `GET /`, take the `href` of the `data-test="browse-products-link"` anchor with the text
  "Browse Products", `GET` that target, and assert `<h1>Our Products</h1>`. This mirrors
  `ProductPageTest.BackToProductsReturnsToTheCatalogue`.
  - Lives in: `Index.cshtml:7` (`<a … href="/products" data-test="browse-products-link">Browse Products</a>`) and
    `Catalog.cshtml:8`.
  - Covered by: **none**. `CatalogueTest.cs:86` asserts the heading "Our Products" on `/products` directly, not by
    following the home page link.

- **view-cart-links-to-the-cart**: Given the shop has started, When a visitor opens the home page, Then the section
  below the heading offers a "View Cart" link to the cart page `/cart`.
  → level: **integration**: `GET /`, and within `data-test="hero"`, after the `h1`, an anchor
  `data-test="view-cart-link"` with the text "View Cart" and `href="/cart"`. Only the link and its target are
  asserted. The cart page itself is out of scope (`CRT-02`).
  - Lives in: `Index.cshtml:6-9` (`div.hero__actions` below the `h1`; line 8
    `<a … href="/cart" data-test="view-cart-link">View Cart</a>`).
  - Covered by: **none**.

- **why-shop-with-us**: Given the shop has started, When a visitor opens the home page, Then a section "Why Shop With
  Us" shows four features: "Free Shipping" — "Books ship cushioned, posters rolled in a tube, hexagons in a fitted
  box. Free over €50."; "Secure Payments" — "Card or invoice, encrypted end to end. Your payment details never touch
  our order history."; "Built to Last" — "Beech and oiled oak, hard enamel, heavyweight cotton. Objects that survive a
  decade of workshops."; "Easy Returns" — "Not the hexagon you pictured? Send any item back within 30 days for a full
  refund."
  → level: **integration**: `GET /`, section `data-test="features"`: title, exactly four `feature-card`s in this
  order, title and description of each. The text is HTML-decoded because `&#8364;` stands for €.
  - Lives in: `Index.cshtml:12-20` (`h2.section__title` "Why Shop With Us", four `div.feature-card` lines 15-18 with
    `h3.feature-card__title` and `p.feature-card__description`). The texts match the story word for word.
  - Covered by: **none**. `HomeSliderE2eTest.cs:23` uses `data-test="features"` only as a layout anchor for the
    slider.

- **popular-categories**: Given the shop has started, When a visitor opens the home page, Then a section "Popular
  Categories" shows, as text and not as links: "Books" — "The works this architecture was synthesized from — Evans,
  Vernon, Cockburn, Fowler, Martin."; "Modeling" — "Sticky notes, hexagon magnets, posters and card decks for your
  next design workshop."; "Apparel" — "Shirts, hoodies and caps that explain your architecture before you open your
  laptop."; "Desk & Office" — "Mugs, coasters, a hex-grid notebook, and the wooden hexagon for your desk.";
  "Stickers & Pins" — "Small enough for a laptop lid, loud enough for a conference hallway."
  → level: **integration**: `GET /`, section `data-test="categories"`: title, exactly five `highlight-card`s in this
  order, title and description of each (HTML-decoded, because `&amp;`), and **no `<a` inside the section**.
  - Lives in: `Index.cshtml:21-30` (`h2.section__title` "Popular Categories", five `div.highlight-card` lines 24-28,
    the title a `span.highlight-card__title` and not an anchor). The texts match the story word for word.
  - Covered by: **none**.

- **shop-now-opens-the-catalogue**: Given a visitor on the home page, below "Ready to Draw Some Boundaries?" and
  "Browse the full catalog: the books, the modelling supplies, and the hexagons.", When they follow "Shop Now", Then
  the catalogue page "Our Products" opens.
  → level: **integration**: `GET /`. In `data-test="cta-section"`, assert the heading and subtitle, then follow the
  `data-test="shop-now-link"` anchor with the text "Shop Now" and assert `<h1>Our Products</h1>` on its target.
  - Lives in: `Index.cshtml:31-37` (`h2.hero__title` line 33, `p.hero__subtitle` line 34, line 35
    `<a … href="/products" data-test="shop-now-link">Shop Now</a>`) and `Catalog.cshtml:8`.
  - Covered by: **none**.

Gaps for the test stage: no scenario has a test at its planned level. Adopt mode changes no existing test. The
covering tests are all new: one browser test in `tests/DcaShop.E2eTests` for the happy path, and six HTTP-level tests
in `tests/DcaShop.IntegrationTests`, for example a `HomePageTest` next to `CatalogueTest`.

## Files

- `src/DcaShop.Web/Views/Home/Index.cshtml` — read: all the markup and `data-test` hooks the criteria observe (`hero`,
  `browse-products-link`, `view-cart-link`, `features`, `categories`, `cta-section`, `cta-banner`, `shop-now-link`).
- `src/DcaShop.Portal/Adapter/Incoming/Web/HomePageController.cs` — read: the `GET /` route.
- `src/DcaShop.Web/Views/Shared/_Layout.cshtml` — read: how the tab title is rendered (line 9).
- `src/DcaShop.Web/Views/Product/Catalog.cshtml` — read: the "Our Products" heading the links lead to (line 8).
- `tests/DcaShop.IntegrationTests/CatalogueTest.cs` — read: the pattern to mirror (HTTP-level page parsing with the
  `Text`/`Section` helpers against `WebApplicationFactory<Program>`; the heading check at line 86).
- `tests/DcaShop.IntegrationTests/ProductPageTest.cs` — read: how to follow a link to the catalogue (`LinkTarget`,
  lines 69-70 and 85).
- `tests/DcaShop.E2eTests/Pages/HomePage.cs` — read: the home page object. It covers only the hero wait and the
  slider today, so the happy-path test needs accessors for the heading, subtitle and description.
- `tests/DcaShop.E2eTests/HomeSliderE2eTest.cs` — read: an existing browser test on the home page (`BaseE2eTest`,
  `E2eFact` with `DisplayName`).

## Glossary proposals

None. The criteria use no new domain term. The Portal glossary already has `HomePage`. Its definition ("Displays the
application title and navigation elements for products, cart, and checkout") is broader than the page renders: the
page has no checkout link. Correcting that is the document stage's job and is not part of this plan.

## Open assumptions

- **Finding: the story's `## Out of scope` contradicts the code.** It says "A product slider on the home page: not
  part of the shop today". But the page renders one: `Index.cshtml:11` (`@await Component.InvokeAsync("ProductSlider")`,
  `src/DcaShop.Product/Adapter/Incoming/Web/ProductSliderViewComponent.cs`). It is covered by
  `HomeSliderE2eTest`, `HomeSlider{Phone,SmallTablet,Tablet}E2eTest` and `tests/DcaShop.IntegrationTests/ProductSliderTest.cs`
  (story `product-slider`, `tasks/product-slider/`), and the Portal glossary lists it. The slider is out of scope
  either way, so this does not block adopting CAT-04's scenarios. No scenario here asserts the order of the page
  sections, so the slider between hero and features contradicts no criterion. The backlog line should be corrected
  (for example, "covered by story `product-slider`") through `/factory-backlog`.
- The browser suite implements only the specification's `scenarios.md` (`AGENTS.md`, "Shared semantics since WP-39";
  `SharedScenariosTest` checks this when `-p:SpecificationPath` is set). The plan assumes that the happy path's
  browser test has, or gets, a matching scenario title in `../dca-sample-specification/scenarios.md`, so the switch
  stays green. The Java suite needs the same test under the same title.
- The story's Notes cite "Inventory: portal H1–H5". The plan assumes this refers to the replay inventory the adopted
  backlog is drawn from and adds no criteria.
