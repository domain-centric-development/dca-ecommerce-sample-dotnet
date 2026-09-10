# Build — STORY-P1

Stage carrier: the stack profile names `carrier.build: ddd-modelling`; the skill is installed here and was read.
Its discipline was applied — the specification is a `sealed record` implementing the context's own
`IStockLevelSpecification`, the domain stays synchronous and framework-free, the port is declared in
`Application/Shared/` and implemented in the outgoing adapter, and the existing types
(`HasMinTotal`, `ICartSpecificationVisitor`, `IShoppingCartRepository`, `GetStockForProductsUseCase`) were read
first so the new code matches the project's idiom rather than the skill's examples.

Knowledge source: the profile names `knowledge: dca-knowledge`. This stage decided nothing the plan had not
already decided and cited; the shapes were taken from the plan and from the project's own precedents.
`factory:ask` ships a vendored catalog, is not named in the profile and was not used.

## Changed

| File | Why |
| --- | --- |
| `src/DcaShop.Inventory/Domain/Specification/AvailableQuantityBelow.cs` | The rule itself: `IsSatisfiedBy` compares `AvailableQuantity.Value` strictly against the threshold; `Accept` hands the leaf to a stock visitor and falls back to the generic composition, as `HasMinTotal` does. |
| `src/DcaShop.Inventory/Domain/Specification/IStockLevelSpecificationVisitor.cs` | New — the push-down hook the plan named, one `Visit` per leaf, mirroring `ICartSpecificationVisitor`. |
| `src/DcaShop.Inventory/Application/Shared/IStockLevelRepository.cs` | Gains `FindAllAsync` and a default `FindByAsync(ICompositeSpecification<StockLevel>)` that filters in memory, so an adapter can adopt push-down later. |
| `src/DcaShop.Inventory/Adapter/Outgoing/Persistence/InMemoryStockLevelRepository.cs` | Implements `FindAllAsync` over the id-keyed store. |
| `src/DcaShop.Inventory/Application/GetLowStockProducts/GetLowStockProductsUseCase.cs` | The read use case: builds `AvailableQuantityBelow(StockQuantity.Of(Threshold))`, asks the port, maps each match to the result's part record. No transaction, as the plan specified. |

Untouched although the plan listed them as changed or the test stage created them:
`IStockLevelSpecification`, `GetLowStockProductsQuery`, `GetLowStockProductsResult`,
`IGetLowStockProductsInputPort` and `InventoryContextRegistration` already carried their final shape from the
test stage — no line of them needed to change to make the tests green.

## Criteria

- `lists-products-below-the-threshold`: met by `GetLowStockProductsUseCase.ExecuteAsync` asking
  `IStockLevelRepository.FindByAsync` with `AvailableQuantityBelow`, whose `IsSatisfiedBy` answers true for every
  stock level holding less than the threshold, and returning one `LowStockProduct` per match.
- `excludes-products-at-or-above-the-threshold`: met by the comparison being strict (`<`), so a stock level at the
  threshold and every level above it fails the specification and never reaches the result list.
- `names-the-available-quantity`: met by `LowStockProduct(stockLevel.ProductId, stockLevel.AvailableQuantity.Value)` —
  each named product carries the quantity it actually holds, as an `int`, never the aggregate.
- `empty-answer-when-nothing-is-low`: met by `FindByAsync` answering an empty list when nothing matches and the use
  case wrapping it in a `GetLowStockProductsResult` with no products — no exception, no null.

## Deviations from the plan

- `Domain/glossary.md` is listed in the plan's change table but was **not** touched here. Documents are the
  documentation stage's output, and this run was asked for the build stage only. The two terms the plan proposed
  (*Low stock*, *Stock threshold*) are still to be written, together with the plan's first open assumption
  (reservations do not count), which the code and `StockLevelSpecificationTest` now pin.
- A negative `GetLowStockProductsQuery.Threshold` fails inside `StockQuantity.Of` with
  `ArgumentException("Stock quantity cannot be negative")` rather than answering an empty list. No criterion names
  the case and no test covers it; the refusal comes from the domain value object the plan chose for the leaf and was
  not designed here. Worth a decision if the overview ever gets a caller that can pass arbitrary input.

## Checks

- `dotnet build` — 0 warnings, 0 errors.
- `dotnet test tests/DcaShop.UnitTests` — 137 passed, 0 failed, 3 skipped (the shared-specification vectors, skipped
  without `-p:SpecificationPath`). All three `StockLevelSpecificationTest` cases green.
- `dotnet test tests/DcaShop.IntegrationTests` — 40 passed, 0 failed, 1 skipped (`RetainedDeliveryTest`, same
  reason). All four `LowStockOverviewTest` cases green.
- `dotnet test tests/DcaShop.ArchitectureTests` — 118 passed, 0 failed. `docs/context-map.md` was regenerated
  unchanged.
- `dotnet test tests/DcaShop.E2eTests` — 18 skipped, 0 failed (no `E2E_BASE_URL`; the suite is untouched by this
  story).
- Formatter: the stack profile declares no format command, so none was run.
- Run without `-p:UseLocalDcaDotnet=true`, i.e. against the published `DomainCentric.BuildingBlocks` 0.1.1 and
  `DomainCentric.ArchRules.Xunit` 0.4.0 that CI uses.
