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
```

The architecture tests also (re)generate `docs/context-map.md` — commit it with the change that caused it.

## Tech stack

.NET 10 (LTS; SDK pinned via `global.json`), ASP.NET Core MVC + Razor views, xUnit, `DomainCentric.BuildingBlocks` + `DomainCentric.ArchRules(.Xunit)`
from `../dca-dotnet` (project references while unpublished; NuGet afterwards). In-memory persistence only.

## Structure and conventions

- Root namespace `DcaShop`; one **project per bounded context** (`DcaShop.Product`, `DcaShop.Cart`,
  `DcaShop.Checkout`), plus `DcaShop.SharedKernel`, `DcaShop.Infrastructure`, `DcaShop.Web`.
- A context is declared by a marker class in its root namespace (`CartContext`) carrying `[BoundedContext]`
  and the context-map attributes (`[Upstream]`, `[ExternalUpstream]`, `[Partnership]`). Context references in
  those attributes use the namespace segment (`"Product"`, `"Cart"`).
- Layers are folders/namespaces: `Domain/Model`, `Domain/Event`, `Domain/Service`, `Application/<UseCase>/`,
  `Application/Shared/` (output ports only), `Adapter/Incoming/{Web,Event}`, `Adapter/Outgoing/<Concern>/`,
  `Api/` (Open Host Service), `Events/` (integration events, consumer-defined trigger interfaces),
  `Infrastructure/` (DI registration `Add<Context>Context()`).
- Naming: `I<Name>InputPort : IUseCase<TCommand|TQuery, TResult>`, `<Name>UseCase`, `<Name>Command` (writes) /
  `<Name>Query` (reads), `<Name>Result`; repositories `I<Aggregate>Repository` / `InMemory<Aggregate>Repository`;
  web adapters `*PageController` + `*PageViewModel`; event adapters `*EventConsumer` (incoming) and
  `*EventPublisher` (domain → integration relay, outgoing); domain events in past tense, integration events
  with the `Event` suffix and `[IntegrationEventType]`.
- Ports and use cases are **async only** (`Task<TOut> ExecuteAsync(TIn, CancellationToken)`, `*Async` methods);
  the **domain stays synchronous**. Value objects are `sealed record`s, ids `readonly record struct : IId`.
- Cross-context calls go **only** through the other context's `Api/` from an outgoing adapter (ACL); consumed
  integration events arrive in `Adapter/Incoming/Event`. Incoming web adapters touch only their own context.
- Events: domain events are dispatched in-process synchronously (`InProcessDomainEventPublisher`);
  integration events are registered in `IIntegrationEventOutbox` and delivered by `IntegrationEventDispatcherService`
  — asynchronous, after the publishing use case finished, at least once with retry/backoff; consumers are idempotent. Listeners implement `EventListener<TEvent>` and are registered as `IEventListener`.
- Views mirror the Java sample's Pug templates one to one (classes, `data-test` attributes, routes, seed data);
  `wwwroot/css/main.css` and `wwwroot/images/products/` are copies of the Java static assets — keep them in sync
  when the Java UI changes. Host-level `HomePageController`/`ErrorPageController`/`MiniBasketViewComponent`
  stand in for the Portal context until stage 2.
- Transactions: writing use cases wrap load → mutate → `SaveAsync` → `PublishAndClearEventsAsync` in
  `IUnitOfWork.RunAsync`; ports that may leave the process (other contexts' data ports, payment providers) are
  called **before** the unit of work, never inside it (ADR-004). Read use cases run without one.
- DI is explicit: every use case, adapter and listener is registered in the context's `*ContextRegistration`.

## Stage-1 stand-ins (remove when the contexts arrive)

- Pricing and Inventory do not exist yet. The Product Catalog answers `IPricingDataPort` /
  `IProductStockDataPort` from `InMemoryPricingDataAdapter` / `InMemoryStockDataAdapter`, seeded by
  `SampleDataSeeder`, and its Api exposes `ProductArticleInfo` (with price and stock) so Cart and Checkout have a
  single source. With real contexts the Api drops price/stock and Cart/Checkout gain `[Upstream("Pricing")]`
  and `[Upstream("Inventory")]`.
- Customers are guests identified by a cookie (`GuestCustomer`) until the Account context exists.
- No cart↔checkout sync on cart changes during an active checkout (Java: `SyncCheckoutWithCart`).

## Sync duty

This repository is one of several artifacts describing the same architecture. When patterns, names or rules
change here, keep the Java sample, the guide and `planning/porting-status.md` in the parent working directory
in step (see the root `AGENTS.md` there). ADRs in `docs/architecture/adr/` are local to this sample and are
not a knowledge-catalog source.
