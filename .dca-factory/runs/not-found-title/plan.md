# Plan — not-found-title: The not-found page carries the shop's name as its title

## Context

The story names `portal`. Portal is on both maps: the designed map (`project/domain.md`, table
"Bounded Contexts": "Web portal, UI composition, cross-context views", Generic (UI)) and the generated
one (`docs/architecture/context-map.md`, row `Portal`). The two agree: Portal has no declared
relationship (Separate Ways on the designed map, no edge on the generated one). The story adds none.

Finding, not settled here: the not-found page is not built in the Portal project. It is rendered by
`ErrorPageController` in the web host (`src/DcaShop.Web/Controllers/ErrorPageController.cs:6-13`,
`code == 404` → `~/Views/Error/404.cshtml`), reached through `app.UseStatusCodePagesWithReExecute("/error/{0}")`
(`src/DcaShop.Web/Program.cs:86`). Portal's glossary lists only `HomePage` as its own term
(`src/DcaShop.Portal/Domain/glossary.md`, "Own Terms"). The plan changes the view where it lives and
moves nothing.

How `/products/no-such-product` reaches that page: `ProductPageController.Detail` is routed
`{productId:guid}` (`src/DcaShop.Product/Adapter/Incoming/Web/ProductPageController.cs:31`). A
non-guid segment matches no route, so the response is a 404, which status-code pages re-execute as
`/error/404`. A guid that is not in the catalogue takes the same path through `return NotFound()`
(line 37). No Product-context code changes.

Why the title reads "Page Not Found" today: the committed view sets
`@{ ViewData["Title"] = "Page Not Found"; }` (`git show HEAD:src/DcaShop.Web/Views/Error/404.cshtml`, line 1).
The layout falls back to the shop's name when no title is set:
`<title>@(ViewData["Title"] ?? "domaincentric.commerce")</title>`
(`src/DcaShop.Web/Views/Shared/_Layout.cshtml:9`).

Project description: `project/product.md` § Surfaces lists server-rendered shopper pages, and
§ Look and feel requires stable `data-test` attributes. `project/tech.md` § Frontend approach says
server-rendered Razor views with no client framework. Both cover this change, which is one line in
a Razor view. The story names no page size.

Carrier: the profile names no `carrier.plan`, so this stage ran in-session. The profile's knowledge
skill (`dca-knowledge`) was not consulted: the story raises no pattern question. No port, layer or
building block is involved.

## Changes

| Element | Kind | Location | New or changed |
| --- | --- | --- | --- |
| Not-found view (`404.cshtml`) | Razor view of the web host's status-code page | `src/DcaShop.Web/Views/Error/404.cshtml` | changed: drop the `ViewData["Title"] = "Page Not Found"` assignment so the layout's fallback, the shop's name, becomes the title. The "404" code, the heading "Page Not Found", the message and both links stay as they are |
| `NotFoundPage` page object | E2E page object | `tests/DcaShop.E2eTests/Pages/NotFoundPage.cs` | new: navigates to a path and reads the document title (mirrors `ProductCatalogPage.NavigateToAsync` / `DocumentTitleAsync`) |

No domain, application, port or adapter element changes. No use case, event or read model is involved.

## Acceptance criteria

- not-found-tab-reads-the-shop-name: Given the seeded sample catalogue, in which no product has the
  id "no-such-product"; When a visitor opens the product page `/products/no-such-product`; Then the
  browser tab title is "domaincentric.commerce".
  →  level: e2e — Playwright (`e2eTest: dotnet test tests/DcaShop.E2eTests`, `browser: playwright`), **happy path** as the story marks it

Criteria for the story's other concrete details (answered in CAT-03-01, see the story's § Assumptions):

- not-found-tab-reads-the-shop-name: the page still shows the code "404", the heading "Page Not Found",
  the message "The page you're looking for doesn't exist or has been removed. Perhaps you were
  looking for one of our products?", and the links "Browse All Products" (`/products`,
  `data-test="error-browse-link"`) and "Go to Homepage" (`/`, `data-test="error-home-link"`).
  These are asserted in the same happy-path test, under the same key. The story has only one
  scenario, so no second key is invented.

## Changed tests

| Test file | Backed by |
| --- | --- |
| — | No committed test asserts the not-found page's title. The only committed not-found test, `tests/DcaShop.IntegrationTests/ShopFlowTest.cs` `UnknownPageRendersThe404Page` (lines 88-97), checks the status 404 and `error-page__code`, and both hold after the change. Nothing is listed, and the story's § Changed expectations ("Page Not Found" → "domaincentric.commerce") is satisfied by the view change alone. |

## Files

- `src/DcaShop.Web/Views/Error/404.cshtml` — changes: remove line 1, the `ViewData["Title"]` assignment
- `tests/DcaShop.E2eTests/NotFoundTitleE2eTest.cs` — changes (new): one `[E2eFact(DisplayName = "The not-found page is titled with the shop's name")]` test
- `tests/DcaShop.E2eTests/Pages/NotFoundPage.cs` — changes (new): page object for the not-found page
- `src/DcaShop.Web/Views/Shared/_Layout.cshtml` — read: line 9, the title fallback the change relies on
- `src/DcaShop.Web/Controllers/ErrorPageController.cs` — read: which view answers a 404
- `src/DcaShop.Web/Program.cs` — read: line 86, status-code re-execution
- `tests/DcaShop.E2eTests/CatalogTitleE2eTest.cs` — read: the pattern for a title test (`BaseE2eTest`, `E2eFact`, `DocumentTitleAsync`)
- `tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs` — read: `NavigateToAsync` / `DocumentTitleAsync` to mirror in `NotFoundPage`
- `tests/DcaShop.IntegrationTests/ShopFlowTest.cs` — read: the existing 404 test, which must stay green

## Open assumptions

- The Java sample's not-found page already carries the shop's name as its title, which is the
  story's "reads the same way in both shops". This plan does not verify it: the Java sample is not
  an input of this stage. If the Java side differs, that is a sync finding for the document stage.
- The shared scenario list (`dca-sample-specification/scenarios.md`) needs the title
  "The not-found page is titled with the shop's name" for `SharedScenariosTest` to accept the new
  E2E test when it runs with `-p:SpecificationPath=…`. The specification is not an input here, and
  the test is skipped without the switch.
- The existing mention of `ErrorPageController` staying in the web host is taken as given. Moving the
  not-found page into Portal is not asked for and is not planned.
