# Judge — STORY-P1

## Verdict
verdict: pass

## Perspectives covered

- **domain**: in-session; the profile names `review.domain: review-domain` and that skill is installed
  (`dca-core:review-domain`), but this run was asked to do the stage in-session without delegation, so the
  perspective's built-in description was applied directly.
- **boundaries**: in-session, same reason (`review.boundaries: review-boundaries` installed, not delegated).
- **craft**: in-session, same reason (`review.craft: review-craft` installed, not delegated).
- No additional perspectives: the profile declares no `reviews:` key, so only the three built-ins ran.

Knowledge source: the profile names `knowledge: dca-knowledge`. This stage decided nothing that needed it — every
judgement below rests on the project's own artifacts: the rule suite (`tests/DcaShop.ArchitectureTests`, 118 rules,
all green), the Cart specification precedent, and `src/DcaShop.Inventory/Domain/glossary.md`. `factory:ask` ships a
vendored catalog, is not named in the profile and was not used.

Suite state re-verified in this session, not taken from `build.md`:

- `dotnet test tests/DcaShop.ArchitectureTests` — 118 passed, 0 failed.
- `dotnet test tests/DcaShop.IntegrationTests` — 40 passed, 0 failed, 1 skipped (`RetainedDeliveryTest`, no
  `-p:SpecificationPath`).
- `dotnet test tests/DcaShop.UnitTests` — 137 passed, 0 failed, 3 skipped (shared-specification vectors).

## Confirmed defects

| Perspective | File:line | Severity | Defect | Fix |
|---|---|---|---|---|
| domain | `src/DcaShop.Inventory/Domain/Specification/AvailableQuantityBelow.cs:24` | minor | The rule compares the quantity *held*, so a stock level of 4 with 4 units reserved is **not** low at a threshold of 4 — the operator sees nothing although nothing is left to promise. This is the story's first open assumption, still unanswered by `domain_contact: warehouse-lead`; the glossary supports both readings (*Available quantity* line 217, *Unreserved quantity (available-to-promise)* line 237). | Get the answer from the warehouse lead before the epic's operator surface is built. If it is available-to-promise, the comparison becomes `AvailableQuantity.Value - ReservedQuantity.Value < Threshold.Value` and `StockLevelSpecificationTest.ComparesTheQuantityHeldRegardlessOfReservations` is the test that flips. |
| test | `tests/DcaShop.IntegrationTests/LowStockOverviewTest.cs:62` | minor | `AnswersWithAnEmptyListWhenNoProductIsShort` reaches its empty answer with `threshold: 0`, which no `StockQuantity` can fall below by construction — the criterion's real case (a positive threshold that every product clears) is never asserted. The test still fails if the use case ignored the specification, so it is not vacuous, only weaker than the criterion. | Give the class its own `IStockLevelRepository` (a `WithWebHostBuilder`/`ConfigureServices` override replacing the singleton) and then assert an empty answer for a positive threshold over a store holding only well-stocked products. |
| craft | `src/DcaShop.Inventory/Domain/Specification/IStockLevelSpecificationVisitor.cs:13` | minor | The visitor has no implementation anywhere and the `Accept` branch in `AvailableQuantityBelow.cs:32-35` is covered by no test. Cart's identical hook is at least exercised by `NameCollectingVisitor` (`tests/DcaShop.UnitTests/Cart/CartSpecificationTest.cs:83`), so this is a push-down hook shipped without the one test that proves it dispatches. | Either add the Cart-style visitor test to `StockLevelSpecificationTest` (a collecting visitor asserting `Visit(AvailableQuantityBelow)` is reached, and the `AndSpecification` fallback for a visitor that does not know the leaf), or drop the visitor until an adapter needs push-down. |

## Considered and dropped

- **`Domain/glossary.md` not updated** (the plan lists it as changed; *Low stock* and *Stock threshold* are not
  in the file). Not a defect of this stage: documents are the documentation stage's output, and `build.md`
  records the deferral explicitly. It becomes a defect only if `stage-document` skips it.
- **No page, no route, no Open Host Service entry** — the epic's outcome (`StockChanged`: an operator *acts*)
  needs a surface, but no acceptance criterion of STORY-P1 names one, and the plan states the omission. Scope,
  not a defect; not a `story-conflict` either, because all four criteria are about the answer, not its delivery.
- **A negative `GetLowStockProductsQuery.Threshold` throws `ArgumentException` from `StockQuantity.Of`**
  (`GetLowStockProductsUseCase.cs:21`). No criterion covers the case, the port has no caller that can pass
  arbitrary input yet, and `build.md` already records it as a decision to take when one arrives. A suspicion
  about a future adapter, not a defect in this change.
- **`AvailableQuantityBelow` carries no `Specification` suffix** — DCA-ADV-017 selects by name only, the Cart
  leaves (`HasMinTotal`, `ActiveCart`, `LastUpdatedBefore`) are named the same way, and the rule suite is green.
  Already enforced by a rule, so not repeated here.
- **`FindByAsync` filters in memory inside the port's default method** — identical to
  `IShoppingCartRepository.FindByAsync` (`src/DcaShop.Cart/Application/Shared/IShoppingCartRepository.cs:34-43`)
  and documented as the step-by-step push-down path. The project's established shape, not a boundary leak: the
  specification is domain-stated and no query technology enters the domain.
- **The `Accept` fallback (`AndSpecification<T>(this, this)`) is duplicated verbatim across six leaves in two
  contexts.** Duplication of plumbing, not of a decision — and lifting it would mean a shared-kernel base type
  for every context's leaf, which is a cross-context change no story asked for. Recorded as an observation.
- **`GetLowStockProductsResult.LowStockProduct` naming and the flat `Application/GetLowStockProducts/` folder** —
  matches DCA-USE-003/006/014/015 and Inventory's existing flat layout; the rule suite covers all four.
- **`FindAllAsync` added to the port although the story needs only `FindByAsync`** — the default `FindByAsync`
  is built on it, and Cart and Product carry the same pair. Not an unused-port-method finding.

## Criteria re-checked

- `lists-products-below-the-threshold`: **met** — `GetLowStockProductsUseCase.cs:21-26` filters with the
  specification and returns one part record per match; the test seeds two products (2 and 3) under a threshold of
  4 and asserts both by id, not by count, so the shared seeded store cannot mask a miss.
- `excludes-products-at-or-above-the-threshold`: **met** — the comparison is strict (`AvailableQuantityBelow.cs:24`)
  and the test pins both the boundary (4 at a threshold of 4) and a value above it (9), plus the unit test
  `DoesNotHoldOnceTheThresholdIsReached`.
- `names-the-available-quantity`: **met** — `LowStockProduct(ProductId, int AvailableQuantity)` and
  `Assert.Equal(3, listed.AvailableQuantity)` against the seeded figure.
- `empty-answer-when-nothing-is-low`: **met only nominally** — the answer is empty and no exception is thrown, but
  the emptiness follows from a threshold no stock level can be below rather than from products being sufficiently
  stocked. See the test finding above; a minor, so it does not block.
