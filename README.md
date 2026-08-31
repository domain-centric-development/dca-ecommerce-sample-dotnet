# dca-ecommerce-sample-dotnet

The .NET reference implementation of **Domain-Centric Architecture (DCA)** — a synthesis of Domain-Driven
Design, Hexagonal Architecture and Clean Architecture. A small webshop built with ASP.NET Core, consuming
`DomainCentric.BuildingBlocks` (the markers) and `DomainCentric.ArchRules` (the executable rules).

It ships seven bounded contexts — **Product Catalog**, **Shopping Cart**, **Checkout**, **Pricing**,
**Inventory**, **Account**, **Portal** — with the same ubiquitous language and use cases as the Java twin
(`dca-ecommerce-sample-java`), plus the **Backoffice** operational module, the REST API and an MCP server.

*Written with AI assistance — drafted mainly by Claude, reviewed and directed by the author since
2025. The architecture rules in this repository's build are part of how that work is verified.*

## Run

Requires the .NET 10 SDK (pinned by `global.json`).

```bash
dotnet build
dotnet test                                   # unit, integration, architecture (112 DCA rules)
dotnet run --project src/DcaShop.Web          # http://localhost:5080
```

Browse `/products`, add items to the cart, check out in five steps (buyer → delivery → payment → review →
confirmation), register an account and log in. All state is in memory; restarting resets the shop, accounts
included.

> Architecture tests must run on **Debug** builds — ArchUnitNET drops the async state machines of optimized
> builds and would miss dependencies inside `async` method bodies. `dotnet test` defaults to Debug.

Until the packages are published on NuGet, the sibling checkout `../dca-dotnet` is referenced as projects
(see `Directory.Build.props`); without it the `PackageReference`s are used.

## End-to-end tests

`tests/DcaShop.E2eTests` drives the shop through a real browser (Playwright, page objects, `data-test`
selectors). They are skipped unless the shop's address is given:

```bash
dotnet run --project src/DcaShop.Web &                      # or any other running instance
E2E_BASE_URL=http://localhost:5080 dotnet test tests/DcaShop.E2eTests
```

`E2E_BROWSER` (`chromium` | `firefox` | `webkit`) and `E2E_HEADLESS=false` are honoured. Because the markup is the
same as the Java sample's, the suite passes against either shop — the browser tests are the language-neutral
acceptance test of the architecture.

## Solution layout

One assembly per bounded context, one for the shared kernel, one for global infrastructure, one web host:

```
src/
├── DcaShop.SharedKernel/        Money, Price, ProductId, UserId; transaction boundary, event dispatch, integration-event outbox
├── DcaShop.Product/             Product Catalog   (namespace DcaShop.Product)
├── DcaShop.Cart/                Shopping Cart     (namespace DcaShop.Cart)
├── DcaShop.Checkout/            Checkout          (namespace DcaShop.Checkout)
├── DcaShop.Pricing/             Pricing           (namespace DcaShop.Pricing)
├── DcaShop.Inventory/           Inventory         (namespace DcaShop.Inventory)
├── DcaShop.Account/             Account           (namespace DcaShop.Account) — accounts, credentials, sessions
├── DcaShop.Portal/              Portal            (namespace DcaShop.Portal) — the landing page; a UI shell with no domain model
├── DcaShop.Backoffice/          operational module, *not* a bounded context — the event publication log
├── DcaShop.Infrastructure/      composition root, outbox dispatcher (retry/backoff), sample data
└── DcaShop.Web/                 ASP.NET Core host: Razor views, layout + mini basket, home/error pages, wwwroot (controllers live in the contexts)
tests/
├── DcaShop.UnitTests/           aggregate and value-object tests
├── DcaShop.IntegrationTests/    browse → cart → checkout through the HTTP pipeline (WebApplicationFactory)
├── DcaShop.ArchitectureTests/   the DCA rule catalog + the executable context map
└── DcaShop.E2eTests/            Playwright browser tests with page objects (run against a started shop)
```

Inside a context project the folders are the DCA layers:

```
DcaShop.Cart/
├── CartContext.cs               [BoundedContext], [Upstream], [Partnership] — the context declaration
├── Domain/
│   ├── Model/                   ShoppingCart (aggregate), CartItem (entity), Quantity, CartArticle, EnrichedCart …
│   ├── Event/                   CartItemAddedToCart, CartCheckedOut, CartCompleted … (past tense)
│   ├── Specification/           ActiveCart, HasMinTotal, LastUpdatedBefore … + ICartSpecificationVisitor
│   └── glossary.md              the context's ubiquitous language
├── Application/
│   ├── AddItemToCart/           I*InputPort : IUseCase<Command, Result>, *UseCase, *Command, *Result
│   ├── GetCartById/             … one folder per use case
│   └── Shared/                  output ports: IShoppingCartRepository, IArticleDataPort
├── Adapter/
│   ├── Incoming/Web/            CartPageController, CartPageViewModel
│   ├── Incoming/Event/          CartCompletionEventConsumer
│   └── Outgoing/                Persistence/ (in-memory repository), Product/ (ACL to Product/Pricing/Inventory), Event/
├── Api/                         CartService — Open Host Service for other contexts
├── Events/                      CartCheckedOutEvent, ICartCompletionTrigger — published language
└── Infrastructure/              CartContextRegistration.AddCartContext() — explicit DI wiring
```

## Same shop, same markup

The pages are a one-to-one translation of the Java sample's Pug templates into Razor: same stylesheet
(`wwwroot/css/main.css`), same product images, same CSS classes and `data-test` attributes, same routes
(`/products`, `/products/{id}`, `/cart`, `/cart/add-product`, POST `/checkout/start`, `/checkout/buyer` →
`delivery` → `payment` → `review` → `confirm` → `confirmation`). The active checkout session is resolved from the
customer, not from the URL. The account pages follow the same rule (`/login`, `/register`, POST `/logout`,
`/account`, `/account/profile`, `/account/change-password`, `/cart/merge`), and so does the backoffice
(`/backoffice/login`, `/backoffice/events`). Two differences in the HTML are intended: the antiforgery hidden
field in every form, and the corner ribbon that names the running implementation
(`.stack-ribbon--dotnet` here, `.stack-ribbon--java` there) — with both shops open in two tabs, the ribbon is
what tells them apart.

## What it demonstrates

| Pattern | Where |
|---|---|
| Aggregate with invariants and domain events | `ShoppingCart`, `CheckoutSession`, `Product` |
| Entity created only through its root | `CartItem` (internal constructor) |
| Value objects as records / `readonly record struct` ids | `Money`, `Quantity`, `CartId`, `BuyerInfo` |
| Factory | `ProductFactory`, `EnrichedCartFactory`, `CheckoutCartFactory` |
| Domain service passed into the aggregate | `TaxCalculator` (contained VAT), `ICheckoutArticlePriceResolver` |
| Enriched read model | `EnrichedProduct`, `EnrichedCart`, `CheckoutCart` / `EnrichedCheckoutLineItem` (persisted line item + fresh article data) |
| Use case = input port + command/query + result | every `Application/<UseCase>/` folder |
| Output ports in `Application/Shared`, adapters outside | `IArticleDataPort` ↔ `CompositeArticleDataAdapter` |
| One port answered from several Open Host Services | `CompositeArticleDataAdapter` — product identity from the catalog, price from Pricing, availability from Inventory |
| Anti-corruption layer to another context's Api | `Adapter/Outgoing/Product/`, `Adapter/Outgoing/Cart/` |
| Open Host Service | `ProductCatalogService`, `CartService` |
| Domain event → integration event relay | `CheckoutConfirmedEventPublisher` → `CheckoutConfirmedEvent` |
| Interface inversion between contexts | `CheckoutConfirmedEvent : ICartCompletionTrigger` (owned by Cart), `ProductCreatedEvent : IPriceInitializationTrigger, IStockInitializationTrigger` |
| Domain service | `CheckoutStepValidator` — decides which checkout step a session may open |
| Read model detached from the aggregate | `CheckoutCartSnapshot`, `LineItemSnapshot` |
| Eventual consistency between contexts | cart changed → `SyncCheckoutWithCart`; checkout confirmed → `ReduceStock` |
| Domain gateway called by the aggregate | `IPasswordHasher` — the contract in `Account/Domain/Gateway`, BCrypt in the adapter |
| Specification as a first-class rule | `UsableDateOfBirth` — evaluated by `Owner` and by the change-profile use case |
| Composable specifications translatable by an adapter | `ActiveCart`, `HasMinTotal`, `HasAnyAvailableItem` … over `ICompositeSpecification<T>`, visited by `ICartSpecificationVisitor` |
| Repository query in domain terms, paged | `IShoppingCartRepository.FindByAsync(specification, PagingRequest)` → `PageResult<ShoppingCart>` |
| Settlement checked against current figures | `ShoppingCart.ValidateForCheckout(IArticlePriceResolver)` → `CartValidationResult`; `CheckoutCartUseCase` refuses a cart whose articles are gone or short in stock |
| Shared-kernel port with one context's implementation | `IIdentityProvider` (shared kernel) resolved by Account's JWT middleware |
| Async at the ports, synchronous domain | `Task<TOut> ExecuteAsync(...)` vs. plain domain methods |
| Executable context map | `docs/context-map.md`, rendered by the architecture tests |
| One protocol per adapter, one set of use cases | `ProductPageController` (Razor), `ProductResource` (REST), `ProductCatalogMcpToolProvider` (MCP) |
| Operational module beside the contexts | `DcaShop.Backoffice` — no context marker, its own authentication |

## API, MCP and the backoffice

Three surfaces sit beside the shop pages.

**REST** (`/api/**`) is authenticated by an `Authorization: Bearer` token and by **nothing else** — on those paths
the identity middleware neither reads a cookie nor writes one, which is the only reason they may skip the
antiforgery token (ADR-007). Authorization is stated by each resource, because the middleware enriches rather than
gates: a request without a token is anonymous, and anonymous is still an identity.

| Route | Who |
|---|---|
| `GET /api/products`, `GET /api/products/{id}` | anyone — the same assortment the shop pages show |
| `POST /api/products` | staff role |
| `GET /api/carts` (every cart in the shop) | staff role |
| `POST /api/carts`, `GET /api/carts/{id}`, `POST /{id}/items`, `DELETE /{id}/items/{itemId}`, `POST /{id}/checkout` | the caller, on their own cart — a stranger's cart answers `404`, never `403` |

Ownership is enforced by the **use cases**, not by the resource: the caller is part of the command and the
repository is asked a scoped question, so the web pages and any future adapter inherit the same rule. The staff
gate is the one check that stays at the edge — it reads only the token's claims, and the same use case is
legitimate for a console with no HTTP identity at all.
| `POST /api/auth/{login,register,logout}` | anyone; the token comes back in the body, no cookie is set |

**MCP** (`/mcp`, `ModelContextProtocol.AspNetCore`) exposes the catalog as the two tools `all-products` and
`product-by-id`, over the same input ports and the same DTO converter as the REST resources — one representation
of a product, not two that can drift. Bearer-only, like `/api/**`.

**Backoffice** (`/backoffice/events`, reachable from the footer) is an operational module, not a bounded context:
it owns no business concept, carries no `[BoundedContext]` marker and does not appear in the context map. It signs
operators in under its own cookie scheme with its own credentials (`admin`/`admin` by default, `Backoffice`
section in `appsettings.json`) — a staff session and a shopper session must never be the same cookie.

> **What the event log actually shows.** The Java sample reads Spring Modulith's `EVENT_PUBLICATION` table: one
> row per *domain* event per listener, completed when that listener returned. This shop has no such registry — its
> domain events are dispatched in-process and leave no record. What it does keep is the **integration-event
> outbox**: one row per integration event, with a status, an attempt count and the last error. The page shows the
> same three numbers (total / completed / incomplete) and the same `data-test` hooks, so the Java browser suite
> passes against it, but the underlying record is not the same one.

## Context map

See [docs/context-map.md](docs/context-map.md) (generated). Cart and Checkout each consume the Product
Catalog through an ACL; Checkout consumes the Cart's Api (ACL), conforms to its `ICartCompletionTrigger`
contract and re-syncs its session on the Cart's `CartContentsChangedEvent`; the Product Catalog reads price and
stock from the Pricing and Inventory Open Host Services, which in turn fill themselves from
`ProductCreatedEvent`; confirming a checkout reduces stock. Cart and Checkout, and Checkout and Inventory, are
partners over the trigger contracts they share.

## Known limitations (by design)

This is an architecture sample, not a production template. Three pieces are intentionally minimal:

- **In-memory persistence** shares mutable aggregate instances between requests and has no optimistic
  concurrency; see ADR-001. `InMemoryTransactionBoundary` draws the boundary and runs after-commit/after-rollback hooks,
  but repositories have nothing to roll back; see ADR-004.
- **The integration-event outbox is in-memory**: at-least-once delivery with retries and a visible `Failed` state,
  but durable only within the process — a restart loses outstanding publications; see ADR-002 for the
  database-backed variant.
- **The staff role has no provisioning path.** `Role.Staff` guards the operator routes of the API, but no
  registration grants it — the tests mint such a token out of band. Authorization also lives in each resource
  rather than in a policy the pipeline enforces; see ADR-007.
- **No refresh token.** The session cookie's lifetime is the blast radius of a stolen token: there is no
  revocation and no theft detection. ADR-006 records the three-cookie design this stops short of, and why.

All three live entirely in adapters — the domain and application layers are unaffected when they are replaced.

## Decisions

Architecture decision records live in [docs/architecture/adr/](docs/architecture/adr/README.md).

## License

MIT — see [LICENSE](LICENSE).

Contributions are accepted under the MIT licence, and the copyright holder may additionally publish
them under other licences (for example a documentation licence for prose).
