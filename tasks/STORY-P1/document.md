# Document — STORY-P1

Stage run in-session, no delegation.

Knowledge source: the profile names `knowledge: dca-knowledge`. This stage decided nothing that needed it — the
glossary shape was taken from the project's own files (`src/DcaShop.Cart/Domain/glossary.md:200-240` as the
precedent for a Specifications section) and every statement from the code. `factory:ask` ships a vendored
catalog, is not named in the profile and was not used.

## Glossary

All rows in `src/DcaShop.Inventory/Domain/glossary.md`.

| Term | Context | Added or changed | Definition source |
|---|---|---|---|
| `IStockLevelSpecification` | Inventory | added, section *Specifications* (was `_None._`) | XML doc on `src/DcaShop.Inventory/Domain/Specification/IStockLevelSpecification.cs:6-13`; wording follows Cart's `ICartSpecification` entry |
| `AvailableQuantityBelow` | Inventory | added, section *Specifications* | `AvailableQuantityBelow.cs:6,19-27` — strictly below, reservations not compared; plan's *Low stock* proposal |
| Low stock | Inventory | added, section *Concepts* | plan's glossary proposal, narrowed to what the code does (`GetLowStockProductsUseCase.cs:21-26`) |
| Stock threshold | Inventory | added, section *Concepts* | plan's glossary proposal; `GetLowStockProductsQuery.cs:4` carries it per request |
| Open Issue 5 — which quantity is "low" | Inventory | added | the story's first open assumption, still unanswered; both readings already in the glossary (*Available quantity*, *Unreserved quantity (available-to-promise)*) |
| Open Issue 6 — whose threshold | Inventory | added | the story's second open assumption; no `StockLevel` member carries a reorder level |

`IStockLevelSpecificationVisitor` is named inside the `IStockLevelSpecification` and
`AvailableQuantityBelow` entries rather than given one of its own — the same treatment Cart's glossary gives
`ICartSpecificationVisitor`.

## Documents updated

| File | What changed | Verified by |
|---|---|---|
| `src/DcaShop.Inventory/Domain/glossary.md` | *Specifications* section filled with the two new domain types; two concepts and two open issues added | every named type read in `src/DcaShop.Inventory/Domain/Specification/` and `Application/GetLowStockProducts/`; `IStockLevelRepository.FindByAsync` read at `src/DcaShop.Inventory/Application/Shared/IStockLevelRepository.cs:26-35` |
| `README.md:149` | the "composable specifications" row named only Cart's leaves and visitor; it now also names `AvailableQuantityBelow` / `IStockLevelSpecificationVisitor`, so the shape does not read as Cart-only | both identifiers exist (`AvailableQuantityBelow.cs:7`, `IStockLevelSpecificationVisitor.cs:14`) |
| `docs/context-map.md` | **unchanged** — no new context, no new relationship; the story reads Inventory's own aggregate | `dotnet test tests/DcaShop.ArchitectureTests` — 118 passed, 0 failed; the suite regenerates the file and `git status --porcelain docs/` is empty afterwards |

## Not documented

- **No page, route, API resource or MCP tool.** Nothing to document: `IGetLowStockProductsInputPort` has no
  adapter, only the DI registration (`InventoryContextRegistration.cs:23`). The epic's operator surface is a
  later story; documenting a surface now would document something that does not exist.
- **Not on the Open Host Service**, so `InventoryService` and the context map's published-interface badge stay
  as they are (`src/DcaShop.Inventory/Api/InventoryService.cs` untouched in the diff).
- **The two open assumptions are recorded as glossary open issues, not resolved.** They wait on
  `domain_contact: warehouse-lead`. The judge's domain finding is the same question; whichever way it is
  answered changes `AvailableQuantityBelow.IsSatisfiedBy` and one unit test, not the ports or the result.
- **A negative `GetLowStockProductsQuery.Threshold`** fails inside `StockQuantity.Of` with
  `ArgumentException("Stock quantity cannot be negative")`. Not documented because no criterion, no caller and
  no test covers it; `build.md` already carries it as a decision to take when an adapter can pass arbitrary
  input. It belongs in an ADR or a story, not in a README.
- **The judge's two remaining minors** (the weak `empty-answer` integration case, the untested visitor dispatch)
  are test-stage work, not documentation. Left where the judge put them.
- **The Java sample's Inventory glossary** carries the same terms by the sync duty in `AGENTS.md`, but it lives
  in another repository and outside this stage's inputs. It waits on a sync pass over
  `dca-ecommerce-sample-java`.
- **`README.md:156`** describes `DcaShop.Backoffice` as having "no context marker", while
  `src/DcaShop.Backoffice/BackofficeContext.cs:20` carries `[BoundedContext("Backoffice", …)]`. Noticed while
  checking this file; unrelated to STORY-P1 and therefore not changed here.
