# Tests — STORY-P1

<!-- gate:tests -->
| criterion | test |
| --- | --- |
| lists-products-below-the-threshold | DcaShop.IntegrationTests.LowStockOverviewTest#NamesEveryProductHoldingLessThanTheGivenQuantity |
| excludes-products-at-or-above-the-threshold | DcaShop.IntegrationTests.LowStockOverviewTest#LeavesOutProductsHoldingTheGivenQuantityOrMore |
| names-the-available-quantity | DcaShop.IntegrationTests.LowStockOverviewTest#StatesHowMuchIsLeftOfEachProductItNames |
| empty-answer-when-nothing-is-low | DcaShop.IntegrationTests.LowStockOverviewTest#AnswersWithAnEmptyListWhenNoProductIsShort |

## Notes

Stage carrier: the stack profile names `carrier.test: e2e-testing`; the skill is installed here and was read.
Its discipline (one flow per test, no assertions on internals, explicit waits, no shared setup between tests) is
applied. Its Page-Object part does not bind: the plan chose the wired-container shape because no criterion names a
route or a page, so there is no markup to hide behind a Page Object. No browser test was written and
`tests/DcaShop.E2eTests` is untouched.

Knowledge source: the profile names `knowledge: dca-knowledge`. This stage decided nothing that needed it — the
shapes were taken from the plan and from the project's own precedents (`CrossContextEventFlowTest`,
`CartSpecificationTest`, `HasMinTotal`). `factory:ask` ships a vendored catalog, is not named in the profile and
was not used.

### Why each test is red

- `NamesEveryProductHoldingLessThanTheGivenQuantity`, `LeavesOutProductsHoldingTheGivenQuantityOrMore`,
  `StatesHowMuchIsLeftOfEachProductItNames`, `AnswersWithAnEmptyListWhenNoProductIsShort`: all four fail with
  `System.NotImplementedException` from `GetLowStockProductsUseCase.ExecuteAsync`
  (`src/DcaShop.Inventory/Application/GetLowStockProducts/GetLowStockProductsUseCase.cs:16`). The container
  resolves `IGetLowStockProductsInputPort`, the seeding through `ISetStockLevelInputPort` runs, and the call
  reaches the use case — so the red is the missing behaviour, not missing wiring.
- The stub refuses rather than answering an empty result on purpose. An empty stub would make
  `empty-answer-when-nothing-is-low` pass vacuously, and a criterion that is green before a line of production
  code exists proves nothing. This is the one deviation from "a stub returns nothing meaningful".

### Unit tests for the invariant

`tests/DcaShop.UnitTests/Inventory/StockLevelSpecificationTest.cs` — the rule `AvailableQuantityBelow`, in the
place and shape `CartSpecificationTest` established:

- `HoldsWhileLessIsHeldThanTheThreshold` — strictly below: quantity 4 is short of a threshold of 5. Currently
  fails on `Assert.True`, because the stub's `IsSatisfiedBy` answers `false` for everything.
- `ComparesTheQuantityHeldRegardlessOfReservations` — the plan's first open assumption (reservations do not
  count) pinned as a test: a fully reserved stock of 4 is still not short of a threshold of 4. Currently fails on
  `Assert.True`. If the warehouse lead answers "available-to-promise" instead, this test is the one that changes.
- `DoesNotHoldOnceTheThresholdIsReached` — the boundary itself is not short. **Green against the stub**, because
  a specification that always answers `false` satisfies a negative assertion. It only starts carrying weight once
  the two tests above are green; it is kept because it is the invariant's other half.

### Stubs added (no behaviour)

`IStockLevelSpecification`, `AvailableQuantityBelow` (`IsSatisfiedBy` → `false`, `Accept` → `NotImplementedException`),
`GetLowStockProductsQuery`, `GetLowStockProductsResult` with the nested part record `LowStockProduct`,
`IGetLowStockProductsInputPort`, `GetLowStockProductsUseCase` (throws), plus one line in
`InventoryContextRegistration` so the integration test resolves the port from the real container instead of
failing on wiring. `IStockLevelSpecificationVisitor` and the repository's `FindAllAsync` / `FindByAsync` are named
in the plan but no test needs them to compile, so the build stage adds them.

`AvailableQuantityBelow` takes a `StockQuantity`, not an `int`: the leaf follows `HasMinTotal(Money)` and gets the
never-negative check for free. `GetLowStockProductsQuery` still carries the `int` the plan specified.

### Test data and isolation

`WebApplicationFactory<Program>` shares one in-memory `IStockLevelRepository` (registered as a singleton) across
the class and with the shop's seed data. Every test therefore seeds products under fresh `Guid`s and asserts only
about its own — `Contains` / `DoesNotContain`, never a count of the whole answer. `AnswersWithAnEmptyListWhenNoProductIsShort`
uses a threshold of `0`, which no stock level can fall below, so it holds whatever else the store contains.
No test depends on the order of the answer (the plan states none is specified).

### Build files

None touched. `tests/DcaShop.UnitTests` has no direct reference to `DcaShop.Inventory`, but gets it transitively
through `DcaShop.Cart` (`src/DcaShop.Cart/DcaShop.Cart.csproj:10`), so the invariant test compiles where the
project's unit tests live.

### Suite state

- `dotnet build` — clean.
- `dotnet test tests/DcaShop.UnitTests` — 2 failed, 135 passed, 3 skipped; both failures are the new ones.
- `dotnet test tests/DcaShop.IntegrationTests` — 4 failed, 36 passed, 1 skipped; all four failures are the new ones.
- `dotnet test tests/DcaShop.ArchitectureTests` — 118 passed, 0 failed. The new namespaces and names break no rule,
  and `docs/context-map.md` was regenerated unchanged.
