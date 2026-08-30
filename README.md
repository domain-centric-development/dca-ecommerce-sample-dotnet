# dca-ecommerce-sample-dotnet

The .NET reference implementation of **Domain-Centric Architecture (DCA)** — a synthesis of Domain-Driven
Design, Hexagonal Architecture and Clean Architecture. A small webshop built with ASP.NET Core, consuming
`DomainCentric.BuildingBlocks` (the markers) and `DomainCentric.ArchRules` (the executable rules).

Stage 1 ships three bounded contexts — **Product Catalog**, **Shopping Cart**, **Checkout** — with the
same ubiquitous language and use cases as the Java twin (`dca-ecommerce-sample-java`). Account, Portal,
Inventory, Pricing and Backoffice follow in later stages.

## Run

Requires the .NET 10 SDK (pinned by `global.json`).

```bash
dotnet build
dotnet test                                   # unit, integration, architecture (111 DCA rules)
dotnet run --project src/DcaShop.Web          # http://localhost:5080
```

Browse `/products`, add items to the cart, check out in five steps (buyer → delivery → payment → review →
confirmation). All state is in memory; restarting resets the shop.

> Architecture tests must run on **Debug** builds — ArchUnitNET drops the async state machines of optimized
> builds and would miss dependencies inside `async` method bodies. `dotnet test` defaults to Debug.

Until the packages are published on NuGet, the sibling checkout `../dca-dotnet` is referenced as projects
(see `Directory.Build.props`); without it the `PackageReference`s are used.

## Solution layout

One assembly per bounded context, one for the shared kernel, one for global infrastructure, one web host:

```
src/
├── DcaShop.SharedKernel/        Money, Price, ProductId, UserId; unit of work, event dispatch, integration-event outbox
├── DcaShop.Product/             Product Catalog   (namespace DcaShop.Product)
├── DcaShop.Cart/                Shopping Cart     (namespace DcaShop.Cart)
├── DcaShop.Checkout/            Checkout          (namespace DcaShop.Checkout)
├── DcaShop.Infrastructure/      composition root, outbox dispatcher (retry/backoff), sample data
└── DcaShop.Web/                 ASP.NET Core host, Razor views (controllers live in the contexts)
tests/
├── DcaShop.UnitTests/           aggregate and value-object tests
├── DcaShop.IntegrationTests/    browse → cart → checkout through the HTTP pipeline (WebApplicationFactory)
└── DcaShop.ArchitectureTests/   the DCA rule catalog + the executable context map
```

Inside a context project the folders are the DCA layers:

```
DcaShop.Cart/
├── CartContext.cs               [BoundedContext], [Upstream], [Partnership] — the context declaration
├── Domain/
│   ├── Model/                   ShoppingCart (aggregate), CartItem (entity), Quantity, CartArticle, EnrichedCart …
│   └── Event/                   CartItemAddedToCart, CartCheckedOut, CartCompleted … (past tense)
├── Application/
│   ├── AddItemToCart/           I*InputPort : IUseCase<Command, Result>, *UseCase, *Command, *Result
│   ├── GetCartById/             … one folder per use case
│   └── Shared/                  output ports: IShoppingCartRepository, IArticleDataPort
├── Adapter/
│   ├── Incoming/Web/            CartPageController, CartPageViewModel
│   ├── Incoming/Event/          CartCompletionEventConsumer
│   └── Outgoing/                Persistence/ (in-memory repository), Product/ (ACL to the catalog Api), Event/
├── Api/                         CartService — Open Host Service for other contexts
├── Events/                      CartCheckedOutEvent, ICartCompletionTrigger — published language
└── Infrastructure/              CartContextRegistration.AddCartContext() — explicit DI wiring
```

## What it demonstrates

| Pattern | Where |
|---|---|
| Aggregate with invariants and domain events | `ShoppingCart`, `CheckoutSession`, `Product` |
| Entity created only through its root | `CartItem` (internal constructor) |
| Value objects as records / `readonly record struct` ids | `Money`, `Quantity`, `CartId`, `BuyerInfo` |
| Factory | `ProductFactory`, `EnrichedCartFactory` |
| Enriched read model | `EnrichedProduct`, `EnrichedCart` |
| Use case = input port + command/query + result | every `Application/<UseCase>/` folder |
| Output ports in `Application/Shared`, adapters outside | `IArticleDataPort` ↔ `ProductCatalogArticleDataAdapter` |
| Anti-corruption layer to another context's Api | `Adapter/Outgoing/Product/`, `Adapter/Outgoing/Cart/` |
| Open Host Service | `ProductCatalogService`, `CartService` |
| Domain event → integration event relay | `CheckoutConfirmedEventPublisher` → `CheckoutConfirmedEvent` |
| Interface inversion between contexts | `CheckoutConfirmedEvent : ICartCompletionTrigger` (owned by Cart) |
| Async at the ports, synchronous domain | `Task<TOut> ExecuteAsync(...)` vs. plain domain methods |
| Executable context map | `docs/context-map.md`, rendered by the architecture tests |

## Context map

See [docs/context-map.md](docs/context-map.md) (generated). Cart and Checkout each consume the Product
Catalog through an ACL; Checkout consumes the Cart's Api (ACL) and conforms to its `ICartCompletionTrigger`
contract; Cart and Checkout are partners over that contract.

## Known limitations (by design)

This is an architecture sample, not a production template. Two pieces of infrastructure are intentionally minimal:

- **In-memory persistence** shares mutable aggregate instances between requests and has no optimistic
  concurrency; see ADR-001. `InMemoryUnitOfWork` draws the boundary and runs after-commit hooks, but there is
  nothing to roll back; see ADR-004.
- **The integration-event outbox is in-memory**: at-least-once delivery with retries and a visible `Failed` state,
  but durable only within the process — a restart loses outstanding publications; see ADR-002 for the
  database-backed variant.

Both live entirely in adapters — the domain and application layers are unaffected when they are replaced.

## Decisions

Architecture decision records live in [docs/architecture/adr/](docs/architecture/adr/README.md).

## License

MIT — see [LICENSE](LICENSE).
