# Plan — STORY-P1: Low stock overview

Stage carrier: the stack profile (`.agents/factory/factory.profile.yaml`) names no `carrier.plan`, so this
stage ran in-session.

Knowledge source: the profile names `knowledge: dca-knowledge`. The in-repo bundle
`../dca-knowledge-catalog/bundle/` was used (resolution order step 2 of the skill). The plugin also ships
`factory:ask` with a vendored catalog; it is **not** named in the profile and was not used.

## Context

**Inventory** (`src/DcaShop.Inventory`, declared by `InventoryContext.cs`, listed in `docs/context-map.md:24`
as "Stock level management and availability tracking"). The story asks which products hold too little stock —
a statement about `StockLevel`, the Inventory aggregate that owns `AvailableQuantity`
(`src/DcaShop.Inventory/Domain/Model/StockLevel.cs:25`). No new context and no new context relationship: the
answer is read from Inventory's own aggregate, and nothing crosses a boundary.

The context map lists Inventory, so the run is not blocked.

## Changes

| Element | Kind | Location | New or changed |
|---|---|---|---|
| `IStockLevelSpecification` | Specification marker interface for the context | `DcaShop.Inventory/Domain/Specification/` | new |
| `IStockLevelSpecificationVisitor<TResult>` | Specification visitor (push-down hook) | `DcaShop.Inventory/Domain/Specification/` | new |
| `AvailableQuantityBelow` | Specification (leaf, domain rule "stock is low") | `DcaShop.Inventory/Domain/Specification/` | new |
| `IStockLevelRepository` | Output port — gains `FindAllAsync` and a default `FindByAsync(ICompositeSpecification<StockLevel>)` | `DcaShop.Inventory/Application/Shared/IStockLevelRepository.cs` | changed |
| `InMemoryStockLevelRepository` | Outgoing persistence adapter — implements `FindAllAsync` | `DcaShop.Inventory/Adapter/Outgoing/Persistence/` | changed |
| `IGetLowStockProductsInputPort` | Input port `IUseCase<GetLowStockProductsQuery, GetLowStockProductsResult>` | `DcaShop.Inventory/Application/GetLowStockProducts/` | new |
| `GetLowStockProductsQuery` | Query (`int Threshold`) | `DcaShop.Inventory/Application/GetLowStockProducts/` | new |
| `GetLowStockProductsResult` | Result with nested part record `LowStockProduct(ProductId, int AvailableQuantity)` | `DcaShop.Inventory/Application/GetLowStockProducts/` | new |
| `GetLowStockProductsUseCase` | Read use case — no transaction | `DcaShop.Inventory/Application/GetLowStockProducts/` | new |
| `InventoryContextRegistration` | DI registration of the new input port | `DcaShop.Inventory/Infrastructure/InventoryContextRegistration.cs:21` | changed |
| `Domain/glossary.md` | Two new terms (see below) | `DcaShop.Inventory/Domain/glossary.md` | changed |

### Why a specification and not a plain finder

The catalog's discriminator asks whether the rule is a *named domain concept*: "the stock of this product is
low" is exactly that, and the story's second open assumption (a per-product reorder level) is a change to that
rule, not to the fetch. The default for a one-shot fetch would be a repository finder; the rule being named and
likely to be recombined tips it to a Specification, which is also the shape this project already uses for query
rules (`src/DcaShop.Cart/Domain/Specification/HasMinTotal.cs:8`, consumed through
`IShoppingCartRepository.FindByAsync`, `src/DcaShop.Cart/Application/Shared/IShoppingCartRepository.cs:34-43`).

  — [Decision] `/decision/specification-vs-query-method.md` (`review: draft` — non-normative, cited as a
    proposal; the project's own Cart precedent is what the shape follows)
  — [Recipe] `/recipe/add-a-read-model.md` step 1–2 (`review: draft`, same caveat): a plain query use case over
    the domain repository is the default read shape; no dedicated read side is justified here.

`AvailableQuantityBelow` carries no `Specification` suffix, matching the leaves in Cart. DCA-ADV-017 selects
**by name only** ("Non-interface types … whose simple name ends with Specification"), so a domain-phrase name is
outside its selection and the suffix is not required.
  — [Rule] `/rule/advanced/dca-adv-017.md`, enforced_by `AdvancedPatternRules#DCA-ADV-017` (status: enforced)

### Rules the change has to satisfy

- Query ends in `Query`, result ends in `Result`, both in the application namespace.
  — [Rule] `/rule/usecase/dca-use-003.md`, `/rule/usecase/dca-use-006.md` (both enforced)
- `GetLowStockProductsResult` must not carry `StockLevel`: the part record holds `ProductId` and an `int`, never
  the aggregate. Checked transitively through nested part records.
  — [Rule] `/rule/usecase/dca-use-015.md`, enforced_by `UseCaseRules#DCA-USE-015` (enforced)
- Inventory's application layer is **flat** today (`Application/GetStockForProducts/`, `Application/ReduceStock/`,
  `Application/SetStockLevel/`), so the new folder is flat as well — one context, one depth.
  — [Rule] `/rule/usecase/dca-use-014.md` (enforced)
- Read use case, so no `ITransactionBoundary` — the same shape as
  `GetStockForProductsUseCase` (`src/DcaShop.Inventory/Application/GetStockForProducts/GetStockForProductsUseCase.cs:6`).
- Domain stays framework-free; the specification lives in `Domain/`, the port in `Application/Shared/`, the
  implementation in the outgoing adapter.

## Acceptance criteria

- `lists-products-below-the-threshold`: Asking for the products below a threshold returns every product whose
  available quantity is lower than that threshold.
  → test shape: integration test against the wired application — `WebApplicationFactory<Program>` +
    `CreateScope()`, resolving `IGetLowStockProductsInputPort` from the real container, after seeding stock
    through the Inventory Open Host Service. Runner: `dotnet test tests/DcaShop.IntegrationTests`
    (profile key `test`). Precedent: `tests/DcaShop.IntegrationTests/CrossContextEventFlowTest.cs:30-48`.
- `excludes-products-at-or-above-the-threshold`: A product whose available quantity equals or exceeds the
  threshold is not part of the answer.
  → test shape: same integration test project, one case seeding a product exactly *at* the threshold and one
    above it. Runner: `dotnet test tests/DcaShop.IntegrationTests`.
- `names-the-available-quantity`: Each product in the answer names its available quantity, so the operator can
  tell how urgent it is.
  → test shape: same integration test project, asserting the `AvailableQuantity` of the returned
    `LowStockProduct` against the seeded figure. Runner: `dotnet test tests/DcaShop.IntegrationTests`.
- `empty-answer-when-nothing-is-low`: When no product is below the threshold, the answer is empty and not an
  error.
  → test shape: same integration test project, asserting an empty collection and no exception. Runner:
    `dotnet test tests/DcaShop.IntegrationTests`.

Invariant-level unit tests (not criteria, but the boundary the criteria rest on): `AvailableQuantityBelow` is
**strictly** below — satisfied at threshold − 1, not at the threshold — in
`tests/DcaShop.UnitTests/Inventory/`. Runner: `dotnet test tests/DcaShop.UnitTests`.

## Glossary proposals

- **Low stock**: A stock level whose available quantity is strictly below a threshold the asker gives; the state
  an operator must react to before the product runs out. Realised by `AvailableQuantityBelow`.
- **Stock threshold**: The quantity, supplied by the asker, at or above which a stock level is not low. Not a
  property of a product — see the open assumption below.

Both are stated against the glossary's existing concept **Available quantity**
(`src/DcaShop.Inventory/Domain/glossary.md:217-224`), which the criteria's "available quantity" means literally:
the quantity held, *regardless of reservations*.

## Open assumptions

- **Reserved quantity does not count** (story's first open question). The plan reads
  `StockLevel.AvailableQuantity` as the glossary defines it — physically held, reservations ignored — because
  that is the term the criteria use and the one the glossary already carries. If the warehouse lead means
  available-to-promise (`AvailableQuantity − ReservedQuantity`, glossary line 239), the specification changes;
  the ports, result and tests do not.
- **One threshold for all products** (story's second open question). `GetLowStockProductsQuery` carries the
  threshold, and no product carries a reorder level. A per-product reorder level would be a new value on
  `StockLevel` and a second specification — out of scope until the question is answered.
- **No operator page and no API endpoint.** No criterion names a route, a page or a response body, so none is
  planned. That is why the end-user shape is the wired-container integration test and not a browser test: the
  project does have a Playwright runner (`tests/DcaShop.E2eTests`), but pointing it at this story would require
  inventing a UI the story does not ask for. The epic's outcome ("an operator acts on a low stock level") will
  need such a surface in a later story.
- **No paging.** Cart's `FindByAsync` takes a `PagingRequest` and answers a `PageResult`; no criterion here asks
  for a page, so `IStockLevelRepository.FindByAsync` answers a plain `IReadOnlyList<StockLevel>`. If the
  overview is later expected to page, it adopts Cart's shape.
- **Not exposed on the Open Host Service.** `InventoryService` (`src/DcaShop.Inventory/Api/InventoryService.cs:11`)
  publishes stock to *other contexts*; the operator overview is not a cross-context need and no criterion asks
  for it, so the use case is not added there.
- **No ordering.** The story specifies no order for the answer, so none is planned; the tests must not depend on
  one.
