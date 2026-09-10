# AGENTS.md

Guidance for AI coding agents working in this repository. It is the **.NET twin** of the Java reference
implementation `dca-ecommerce-sample-java`; both demonstrate the same Domain-Centric Architecture.

## Principles that apply in every DCA repository

These hold for anyone working in any of the DCA repositories — human or agent — regardless of local tooling or memory.

1. **The samples exist to make the AI harness deterministic, not to ship features.** They are the experiment field
   for the harness: knowledge catalog, architecture rules, markers, plugins. Every architectural change in a sample
   answers three questions before it is done — does the catalog need a node (pitfall / decision / recipe /
   template)? could an ArchUnit / ArchUnitNET rule check it (same rule id in `dca-java` and `dca-dotnet`)? would one
   more *generic* marker in the building blocks make it checkable? Record the answer, "none" included, in the WP or
   ADR. Two agents building the same thing differently in the two samples is a determinism gap to close at the
   source (catalog, rule, marker), not with a code fix.
2. **Rules, markers and the catalog are general.** They are the foundation other production systems — any industry —
   build on with AI. Nothing in them may exist only because the e-commerce sample needs it: no shop vocabulary in
   rule or marker names or texts, no selection that only matches the sample's layout, no catalog node that presupposes
   a cart. The sample proves the general artifact; it is never its source of names or shapes.
3. **The core libraries are framework-neutral.** `dca-building-blocks` / `DomainCentric.BuildingBlocks` have zero
   dependencies; `dca-archunit` / `DomainCentric.ArchRules` reference frameworks only as configurable presets
   (`FrameworkAnnotations`, `FrameworkTypes`) and never on their class path. Framework-specific code goes into
   satellite artifacts (`dca-spring`, `dca-archunit-spring-modulith`) or into the sample. Spring is the default
   preset and the reference implementation's framework, not the vocabulary of the rules.
4. **Each reader artifact stands alone.** Guide, book and catalog bundle are independently readable; links never
   cross repository boundaries (the sample may cite the guide). `AGENTS.md` files are exempt — they carry the
   cross-project pointers and the sync duties.

5. **History stays.** Old branches, tracked working artifacts (`tasks/`, `.claude/agents/`, `.run/`, `slides/` in the
   Java sample) and superseded documents are kept on purpose as the record of how the sample was built. Do not
   propose deleting or archiving them; audits list them as information, not as findings.

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
docker compose up --build                                                # same shop in a container
docker compose run --rm test                                             # tests without a local SDK
```

The architecture tests also (re)generate `docs/context-map.md` — commit it with the change that caused it.

## Tech stack

.NET 10 (LTS; SDK pinned via `global.json`), ASP.NET Core MVC + Razor views, xUnit, `DomainCentric.BuildingBlocks` + `DomainCentric.ArchRules(.Xunit)`
from NuGet.org (`DomainCentric.BuildingBlocks` 0.1.1, `DomainCentric.ArchRules.Xunit` 0.4.0, pinned in `Directory.Build.props`;
`-p:UseLocalDcaDotnet=true` references the sibling `../dca-dotnet` as projects for unreleased rules — the counterpart of
the Java sample's `-PwithDcaJava`; **run the tests once without it before calling anything done**, and CI
(`.github/workflows/ci.yml`) does exactly that on every push). In-memory persistence only.

## Structure and conventions

- Root namespace `DcaShop`; one **project per bounded context** (`DcaShop.Product`, `DcaShop.Cart`,
  `DcaShop.Checkout`, `DcaShop.Pricing`, `DcaShop.Inventory`, `DcaShop.Account`, `DcaShop.Portal`,
  `DcaShop.Backoffice`), plus `DcaShop.SharedKernel`, `DcaShop.Infrastructure` and `DcaShop.Web`.
  Backoffice is a **generic** subdomain — operating the application itself — and a bounded context since
  2026-09-03: it owns the *event publication* language, so it carries `[BoundedContext]` on
  `BackofficeContext` and appears on the context map as Separate Ways.
- A context is declared by a marker class in its root namespace (`CartContext`) carrying `[BoundedContext]`
  and the context-map attributes (`[Upstream]`, `[ExternalUpstream]`, `[Partnership]`). Context references in
  those attributes use the namespace segment (`"Product"`, `"Cart"`).
- Layers are folders/namespaces: `Domain/Model`, `Domain/Event`, `Domain/Service`, `Domain/Specification`,
  `Application/<UseCase>/` — or, once a context has grown cohesive clusters, `Application/<Feature>/<UseCase>/`
  (a *feature* is an optional, domain-named group of use cases below the layer — Cart: `Shopping`, `CartRecovery`,
  `CartCheckout`, `Operations`; Checkout: `Session`, `CheckoutCompletion`, `CartSync`; one context uses one form,
  `DCA-USE-014`; features must not form cycles, `DCA-CYC-005`; incoming adapters may mirror them *below* the
  protocol: `Adapter/Incoming/Web/<Feature>/`; the domain is never mirrored by feature),
  `Application/Shared/` (output ports only, context-wide — never per feature), `Adapter/Incoming/{Web,Event}`, `Adapter/Outgoing/<Concern>/`,
  `Adapter/Incoming/Api` (REST resources + their DTOs and converters), `Adapter/Incoming/Mcp` (MCP tools),
  `Api/` (Open Host Service), `Events/` (integration events, consumer-defined trigger interfaces),
  `Infrastructure/` (DI registration `Add<Context>Context()`). Each context keeps its ubiquitous language in
  `Domain/glossary.md` — the same glossaries as the Java sample; a renamed or added domain term is changed there
  too.
- Naming: `I<Name>InputPort : IUseCase<TCommand|TQuery, TResult>`, `<Name>UseCase`, `<Name>Command` (writes) /
  `<Name>Query` (reads), `<Name>Result` — values only, never an aggregate root or entity (`DCA-USE-015`); part records
  named by content (`CartItemSummary`), nested in the result; command results small (the checkout wizard's
  `Submit*`/`ConfirmCheckout` answer `(SessionId, CurrentStep, Status)`, the queries return the
  `CheckoutCartSnapshot`); Cart results carry `Money`; step access is decided by the `GetActiveCheckoutSession`
  query and delivered as a `StepAccess` value that the web adapter maps to a route — no `IDomainService` in an
  incoming adapter (`DCA-HEX-012`); repositories `I<Aggregate>Repository` / `InMemory<Aggregate>Repository`;
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
  IReadOnlyDictionary<ProductId, ArticlePrice>)` delegates to pure `CartPricing` over line snapshots and answers a `CartValidationResult`, and `CheckoutCartUseCase` turns a non-empty one into
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
  `IIdentityProvider`. `ShopIdentityAuthenticationHandler` (Account) is an ASP.NET Core authentication scheme
  that resolves it per request into `HttpContext.User` (ADR-008) from two cookies — `shop-identity` (who the
  browser is, 30 days, rotated only on explicit logout) and `shop-session` (the authentication, 7 days, expiry
  harmless). Expiry ends the session, never the identity, so an aged-out login never costs the cart. The handler
  enriches, it does not gate: every request ends with an identity, recorded as `IShopIdentityFeature` on the
  `HttpContext` (that is what `HttpContextIdentityProvider` reads — `HttpContext.User` is replaced by any
  `[Authorize]` that names another scheme, e.g. on backoffice pages, and the layout's mini basket still needs the
  visitor), but only a registered session yields an authenticated principal (`ShopPrincipal.From`). An anonymous
  visitor is `NoResult`, so `[Authorize]` challenges (login redirect on pages, `401` + `WWW-Authenticate: Bearer`
  with a problem document on the API) and a customer without a role is forbidden (`403`). Claims-only gates are
  attributes: `[Authorize(Roles = RoleStaff)]` on the two staff routes, `[Authorize]` on the account pages. The
  backoffice keeps its own cookie scheme, never the default.
- API surface: `/api/**` and `/mcp` are **Bearer only** — the default policy scheme forwards those paths to the
  `ShopBearer` scheme, which reads no cookie and writes none; that is the sole reason `TokenOnlyAwareAntiforgeryFilter`
  may exempt them from the antiforgery token. The path list lives in `TokenOnlyPaths` and both the scheme selector
  and the filter ask it; never give one half a list of its own.
- Authorization: **a guard goes where its inputs are** (ADR-007), not where it feels "business" or "technical".
  A claims-only gate may sit in the adapter — `POST /api/products` and `GET /api/carts` are staff-only there,
  because that is a property of the exposure. An **ownership** check never may: the caller is part of the command
  (`GetCartByIdQuery(CartId, CustomerId)`, `CheckoutCartCommand`, `StartCheckoutCommand`) and the use case asks
  `IShoppingCartRepository.FindByIdForCustomerAsync`, not `FindByIdAsync` plus an `if`. Cart's Open Host Service
  demands the customer too, so Checkout inherits the rule. `FindByIdAsync` stays for the system paths that act on
  nobody's behalf (`CompleteCart`, from an integration event). The refusal is *rendered* at the edge: a stranger's
  cart answers `404`, never `403`.
- The backoffice has its **own** cookie scheme and its own credentials (`Adapter.Incoming.Web.BackofficeOptions`, defaults
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

A domain term such as `PortfolioManager` is valid; the remaining technical suffix
restrictions still apply. Operation implementations are discovered by InputPort
assignability or the configured use-case suffix. Optional organisational segments
are configured with `withOperationContainers(...)` / `WithOperationContainers(...)`
and removed before measuring flat/grouped operation depth. Supporting subfolders do
not define operations. One context must still use one depth. A Repository or Store
used by one use case may live with it; `application/shared` is the reuse default.

WP-34 policy: use-case stereotypes are optional; configuration registration is equally valid.
NAM-002 is a non-failing Java diagnostic, not a wiring guarantee. Outgoing adapters may
reuse global/own infrastructure. Domain metadata rules classify configured roles on
types and members (including composed metadata), allow unclassified metadata, and assign
exclusive ownership to ADV-004/011/015/018 before ONI-003.

## Shared semantics since WP-39

`../dca-sample-specification/` is the semantic authority; read its CONTRIBUTING.md, vectors and checkout-lifecycle.md before
business changes. The user owns semantics. The specification is **unpublished and not part of the build** (decided
2026-09-10): nothing is downloaded, no revision is pinned, and a plain checkout builds without it. The specification tests
run only with `-p:SpecificationPath=<checkout>/dca-sample-specification` and are skipped otherwise; no vector may lack a test
adapter. Update both samples' adapters, schema compatibility records and glossaries together.

An explicit checkout action captures immutable positions, quantities and prices into a session. Cart edits do not
create or mutate sessions. A new action supersedes the previous OPEN/Active session; confirmed/completed orders remain.
Confirmation and replacement serialize through the same repository operation, including transaction completion.
Superseded confirmation has no completion effect. Abandonment/expiry closes only an open session and leaves cart contents.

Cart reconciliation intersects purchased unit intervals with the current stable position id. Later additions (also of
the same product), removed/re-added positions and other contents survive. Replay and overlapping completed snapshots
cannot remove a unit twice. JDBC/JPA cart persistence preserves the interval allocation watermark; in-memory persistence
retains the same domain state. Legacy CheckedOut/Completed cart statuses remain readable, but snapshot checkout leaves
an active cart editable and never completes the whole cart.

Confirmation retrieves current price/availability/stock facts before its local transaction. Pure domain services consume
immutable line/fact snapshots. Any changed price or shortage reports affected lines and leaves state, totals and events
unchanged. The buyer explicitly starts a fresh checkout against the new prices. Success stores the recomputed total and
publishes the same total; there is no no-argument confirmation path. Local in-memory repository serialization is not a
claim of durable distributed transactions or universal rollback of unenlisted resources.

## Delivery pipeline

New work runs through the factory pipeline from `dca-marketplace/plugins/dca-factory`, installed
into this repository (not vendored — `.claude/skills/` is gitignored, re-install with
`factory.sh install`):

- `backlog/<epic>/<story>.md` — new stories; the porting log and the work packages keep the delivered work
- `.agents/factory/factory.profile.yaml` — the only file that tells the pipeline how this project
  builds: `dotnet build`, the unit/integration/E2E test projects and `dotnet test tests/DcaShop.ArchitectureTests` (Debug), plus the knowledge
  source (`dca-knowledge`), the stage carriers and the review perspectives
- `.agents/factory/story-gate.py` — the gate between the stages
  (`--story <id> --stage plan|test|build|document`)
- `.githooks/pre-commit` — the same profile commands on every commit
  (`git config core.hooksPath .githooks`); narrow it with
  `FACTORY_PRECOMMIT_CHECKS="compile architecture"` when the full suite is too slow to wait for

Run one story with `/factory-run <story id>`. The pipeline owns the process; the architecture comes
from `dca-core` (`/dca-bootstrap` once, then `/ddd-modelling`, `/review-*`, `/dca-knowledge`).
