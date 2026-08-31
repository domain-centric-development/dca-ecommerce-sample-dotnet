# AGENTS.md

Guidance for AI coding agents working in this repository. It is the **.NET twin** of the Java reference
implementation `dca-ecommerce-sample-java`; both demonstrate the same Domain-Centric Architecture.

## Documentation language

All persisted artifacts (`*.md`, XML docs, comments, commit messages) are **English**.

## Build & test

```bash
export PATH=$HOME/.dotnet:$PATH          # if dotnet is not on the PATH
dotnet build
dotnet test                              # everything
dotnet test tests/DcaShop.ArchitectureTests   # DCA rule catalog (Debug build required)
dotnet test tests/DcaShop.UnitTests --filter "FullyQualifiedName~ShoppingCart"
dotnet run --project src/DcaShop.Web     # http://localhost:5080
E2E_BASE_URL=http://localhost:5080 dotnet test tests/DcaShop.E2eTests   # Playwright; skipped without E2E_BASE_URL
```

The architecture tests also (re)generate `docs/context-map.md` — commit it with the change that caused it.

## Tech stack

.NET 10 (LTS; SDK pinned via `global.json`), ASP.NET Core MVC + Razor views, xUnit, `DomainCentric.BuildingBlocks` + `DomainCentric.ArchRules(.Xunit)`
from `../dca-dotnet` (project references while unpublished; NuGet afterwards). In-memory persistence only.

## Structure and conventions

- Root namespace `DcaShop`; one **project per bounded context** (`DcaShop.Product`, `DcaShop.Cart`,
  `DcaShop.Checkout`, `DcaShop.Pricing`, `DcaShop.Inventory`, `DcaShop.Account`, `DcaShop.Portal`), plus
  `DcaShop.SharedKernel`, `DcaShop.Infrastructure`, `DcaShop.Web` and `DcaShop.Backoffice` — the last an
  **operational module, not a context**: no `[BoundedContext]` marker, absent from the context map.
- A context is declared by a marker class in its root namespace (`CartContext`) carrying `[BoundedContext]`
  and the context-map attributes (`[Upstream]`, `[ExternalUpstream]`, `[Partnership]`). Context references in
  those attributes use the namespace segment (`"Product"`, `"Cart"`).
- Layers are folders/namespaces: `Domain/Model`, `Domain/Event`, `Domain/Service`, `Domain/Specification`,
  `Application/<UseCase>/`,
  `Application/Shared/` (output ports only), `Adapter/Incoming/{Web,Event}`, `Adapter/Outgoing/<Concern>/`,
  `Adapter/Incoming/Api` (REST resources + their DTOs and converters), `Adapter/Incoming/Mcp` (MCP tools),
  `Api/` (Open Host Service), `Events/` (integration events, consumer-defined trigger interfaces),
  `Infrastructure/` (DI registration `Add<Context>Context()`). Each context keeps its ubiquitous language in
  `Domain/glossary.md` — the same glossaries as the Java sample; a renamed or added domain term is changed there
  too.
- Naming: `I<Name>InputPort : IUseCase<TCommand|TQuery, TResult>`, `<Name>UseCase`, `<Name>Command` (writes) /
  `<Name>Query` (reads), `<Name>Result`; repositories `I<Aggregate>Repository` / `InMemory<Aggregate>Repository`;
  web adapters `*PageController` + `*PageViewModel`; REST adapters `*Resource` (`[ApiController]`,
  `Adapter/Incoming/Api/`) — the layout's `RestControllerSuffix` is set to `Resource` in `ArchitectureRulesTest`
  so `DCA-NAM-006` enforces the Java sample's name rather than the .NET default `Controller`; event adapters `*EventConsumer` (incoming) and
  `*EventPublisher` (domain → integration relay, outgoing); domain events in past tense, integration events
  with the `Event` suffix and `[IntegrationEventType]`.
- Ports and use cases are **async only** (`Task<TOut> ExecuteAsync(TIn, CancellationToken)`, `*Async` methods);
  the **domain stays synchronous**. Value objects are `sealed record`s, ids `readonly record struct : IId`.
- Cross-context calls go **only** through the other context's `Api/` from an outgoing adapter (ACL); consumed
  integration events arrive in `Adapter/Incoming/Event`. Incoming web adapters touch only their own context.
- Article data is composed by the **consumer**, as in the Java sample: `CompositeArticleDataAdapter` (Cart) and
  `CompositeCheckoutArticleDataAdapter` (Checkout) each call three Open Host Services — identity and description
  from `ProductCatalogService`, the price from `PricingService`, availability from `InventoryService` — and
  translate them into the context's own article type. The catalog's Api carries **no** price or stock: it reads
  both as well, but only to present its own pages. A product **nobody has priced** is offered as unavailable
  (price 0, stock 0, `IsAvailable = false`) with a warning in the log — not with an exception: that is the state
  of every product between its creation and Pricing consuming `ProductCreatedEvent`, and a shop must not answer
  it with an error page. Add-to-cart then refuses with "Insufficient stock for product: …", and the checkout
  validation names the line as `ProductUnavailable`. The Java sample behaves the same way.
- Settlement is checked against current figures, not stored ones: `ShoppingCart.ValidateForCheckout(
  IArticlePriceResolver)` answers a `CartValidationResult`, and `CheckoutCartUseCase` turns a non-empty one into
  a `CartValidationException` (the REST resource renders it as `400`). An **empty or inactive** cart is not a
  validation error with no errors: the use case lets the aggregate refuse it, so the reason reads "Cannot
  checkout an empty cart". Same in the Java sample. The article data is fetched **before** the transaction
  (ADR-004).
- Query rules the domain states itself: `Domain/Specification` holds composable specifications over
  `ICompositeSpecification<T>` (shared kernel: `And`/`Or`/`Not` plus `ISpecificationVisitor`), and
  `IShoppingCartRepository.FindByAsync(specification, PagingRequest)` answers a `PageResult<ShoppingCart>`. The
  port's default filters and pages in memory so an adapter can adopt push-down (via `ICartSpecificationVisitor`)
  step by step. What the aggregate cannot see (stock, customer preferences, timestamps) evaluates neutrally in
  memory and belongs to the push-down.
- Events: domain events are dispatched in-process synchronously (`InProcessDomainEventPublisher`);
  integration events are registered in `IIntegrationEventOutbox` inside the use case's transaction, released after commit and delivered by `IntegrationEventDispatcherService`
  — asynchronous, after the publishing use case finished, at least once with retry/backoff; consumers are idempotent. Listeners implement `EventListener<TEvent>` and are registered as `IEventListener`.
- E2E: `tests/DcaShop.E2eTests` is a one-to-one port of the Java `src/test-e2e` page objects and suites
  (`CheckoutGuestE2ETest`, `CheckoutLoginE2ETest`, `CartMergeE2ETest`, `BackofficeE2ETest` — 15 scenarios, same
  `data-test` selectors, same scenario names). Either suite runs against either shop
  (`./gradlew test-e2e -De2e.baseUrl=http://localhost:5080` in the Java repo; `E2E_BASE_URL=http://localhost:8080`
  here). Keep selectors and scenarios in sync with the Java suite.
- Views mirror the Java sample's Pug templates one to one (classes, `data-test` attributes, routes, seed data);
  `wwwroot/css/main.css` and `wwwroot/images/products/` are copies of the Java static assets — keep them in sync
  when the Java UI changes. The one deliberate markup difference besides the antiforgery field is the corner
  ribbon in the layout: `.stack-ribbon--dotnet` here, `.stack-ribbon--java` in the Java sample, both styled by
  the same `.stack-ribbon` block in `main.css`. It exists so two tabs of the same-looking shop can be told
  apart; keep the CSS identical and only the modifier class and the label different. `ErrorPageController` and `MiniBasketViewComponent` stay in the web host; the
  backoffice views live there too (`Views/Backoffice/`), rendered by `DcaShop.Backoffice`'s page controller.
- Transactions: writing use cases wrap load → mutate → `SaveAsync` → `PublishAndClearEventsAsync` in
  `ITransactionBoundary.InTransactionAsync`; ports that may leave the process (other contexts' data ports, payment providers) are
  called **before** the unit of work, never inside it (ADR-004). Read use cases run without one.
- DI is explicit: every use case, adapter and listener is registered in the context's `*ContextRegistration`.
- Identity: every context keys its data on the visitor's `UserId`, read through the shared-kernel port
  `IIdentityProvider`. `JwtAuthenticationMiddleware` (Account) resolves it per request from two cookies —
  `shop-identity` (who the browser is, 30 days, rotated only on explicit logout) and `shop-session` (the
  authentication, 7 days, expiry harmless). Expiry ends the session, never the identity, so an aged-out login
  never costs the cart. The middleware enriches, it does not gate: a page decides who may see it (ADR-006).
- API surface: `/api/**` and `/mcp` are **Bearer only** — on those paths the middleware reads no cookie and
  writes none, which is the sole reason `TokenOnlyAwareAntiforgeryFilter` may exempt them from the antiforgery
  token. The path list lives in `JwtAuthenticationMiddleware.TokenOnlyPathPrefixes` and the filter asks it;
  never give one half a list of its own.
- Authorization: **a guard goes where its inputs are** (ADR-007), not where it feels "business" or "technical".
  A claims-only gate may sit in the adapter — `POST /api/products` and `GET /api/carts` are staff-only there,
  because that is a property of the exposure. An **ownership** check never may: the caller is part of the command
  (`GetCartByIdQuery(CartId, CustomerId)`, `CheckoutCartCommand`, `StartCheckoutCommand`) and the use case asks
  `IShoppingCartRepository.FindByIdForCustomerAsync`, not `FindByIdAsync` plus an `if`. Cart's Open Host Service
  demands the customer too, so Checkout inherits the rule. `FindByIdAsync` stays for the system paths that act on
  nobody's behalf (`CompleteCart`, from an integration event). The refusal is *rendered* at the edge: a stranger's
  cart answers `404`, never `403`.
- The backoffice has its **own** cookie scheme and its own credentials (`BackofficeOptions`, defaults
  `admin`/`admin`). A staff session and a shopper session are never the same cookie.

## Stand-ins still in place (remove when the contexts arrive)

- `ErrorPageController` and `MiniBasketViewComponent` stay in the web host on purpose — the error page belongs
  to no context, and the mini basket composes the Cart's Api into the shared layout.
- The staff role has no provisioning path: nothing grants `Role.Staff`, so an operator token is minted out of
  band (the tests do it through `JwtTokenService`). Deliberate for a sample; ADR-007 records it.
- No refresh token (`shop-refresh`): no revocation, no theft detection, the session lifetime is the blast
  radius. Deliberate and shared with the Java sample; ADR-006 records the design it stops short of.
- Pricing and Inventory arrived in stage 2a: the Product Catalog's `IPricingDataPort` / `IProductStockDataPort`
  are answered by `PricingDataAdapter` / `InventoryStockDataAdapter` calling the real Open Host Services, and a
  new product gets its price and stock through `ProductCreatedEvent` (see the trigger contracts below).
- Consumer-defined trigger contracts (interface inversion, keeps the project graph acyclic): `ICartCompletionTrigger`
  (Cart), `IStockReductionTrigger` (Inventory) — both implemented by `CheckoutConfirmedEvent`;
  `IPriceInitializationTrigger` (Pricing) and `IStockInitializationTrigger` (Inventory) — implemented by
  `ProductCreatedEvent`. The Java sample carries the same four contracts (without the `I` prefix), so the
  creation flow is the same on both sides.
- Backoffice, the REST API and MCP arrived in stage 2c: the footer's Event Log link is live, `/api/**` carries
  the resources of Product, Cart and Account, and `/mcp` exposes the catalog as the tools `all-products` and
  `product-by-id`. The event log is built from the **integration-event outbox**, not from a per-listener domain
  event registry as in Java — the numbers mean the same thing to an operator but count different things, and the
  README and `OutboxEventPublicationLogStore` both say so.
- Account and Portal arrived in stage 2b: `GuestCustomer` and the `dcashop-customer` cookie are gone, the
  landing page moved out of the host into `DcaShop.Portal`, and after a login the Cart context decides for
  itself whether carts have to be merged (`/cart/merge`) or the guest cart simply recovered — Account never
  calls Cart.

## Sync duty

This repository is one of several artifacts describing the same architecture. When patterns, names or rules
change here, keep the Java sample, the guide and `planning/porting-status.md` in the parent working directory
in step (see the root `AGENTS.md` there). ADRs in `docs/architecture/adr/` are local to this sample and are
not a knowledge-catalog source.
