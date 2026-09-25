# Document — product-slider

Carrier for the glossary: `ubiquitous-language` (`carrier.glossary` in `.agents/factory/factory.profile.yaml`),
applied in-session in the glossaries' existing format. Carrier for the designed map: `context-map`
(`carrier.domain`), not invoked, since the story changed no relationship (see below). The profile names the knowledge
source `dca-knowledge`. This stage did not consult it: it made no architecture decision beyond the plan's cited nodes.

## Glossary
| Term | Context | Added or changed | Definition source |
| --- | --- | --- | --- |
| ProductSelection | Product | added: `src/DcaShop.Product/Domain/glossary.md`, section "Value Objects" | plan `## Glossary proposals`, first line — "Product selection (`ProductSelection`, Product context)"; checked against `src/DcaShop.Product/Domain/Model/ProductSelection.cs` (`MaxSize = 4`, `Draw`, distinct ids in draw order) and story `## Assumptions` (out-of-stock products offered when priced) |
| Product slider | Portal (referenced, owned by Product) | added: row in `src/DcaShop.Portal/Domain/glossary.md`, table "Referenced Terms" | plan `## Glossary proposals`, second line — "Product slider (Portal glossary, referenced term owned by `Product`)"; checked against `src/DcaShop.Web/Views/Home/Index.cshtml:11` (`Component.InvokeAsync("ProductSlider")`) and `src/DcaShop.Portal/DcaShop.Portal.csproj` (references only `DcaShop.SharedKernel`) |

## Documents updated
| File | What changed | Verified by |
| --- | --- | --- |
| src/DcaShop.Product/Domain/glossary.md | new entry `ProductSelection` | read `src/DcaShop.Product/Domain/Model/ProductSelection.cs`, `src/DcaShop.Product/Application/GetProductSelection/GetProductSelectionUseCase.cs` |
| src/DcaShop.Portal/Domain/glossary.md | "Product slider" row in "Referenced Terms" | grep `ProductSlider` in `src/DcaShop.Web/Views/Home/Index.cshtml` (line 11); read `src/DcaShop.Portal/DcaShop.Portal.csproj` |
| README.md | section "Same shop, same markup": names the slider (`data-test="product-slider"`, `.product-slider__*` rules) and the `product-detail-title` / `product-detail-price` attributes as a markup difference that exists only in the .NET sample for now | grep `data-test="product-slider"` in `src/DcaShop.Web/Views/Shared/Components/ProductSlider/Default.cshtml:2`; grep `.product-slider__` in `src/DcaShop.Web/wwwroot/css/main.css` (from line 2836); grep `product-detail-title`/`-price` in `src/DcaShop.Web/Views/Product/Detail.cshtml:12,34`; `../dca-sample-specification/exceptions.md:3,7` (approved exception, not cited in the README because reader artifacts do not link across repositories) |

## Not documented
- `project/domain.md` (designed map): unchanged. The story adds no context and no relationship. Portal stays Separate
  Ways and composes in the UI ("Notable design choices" 5 already says so). Product's Pricing/Inventory ACLs are
  unchanged. Verified: `git diff -- src/*/*Context.cs` is empty, and `DcaShop.Portal.csproj` has no new reference.
- `docs/architecture/context-map.md` (generated): the architecture tests write it. `build.md` records "129 passed;
  `docs/architecture/context-map.md` unchanged", and `git diff -- docs/architecture/context-map.md` is empty.
- The rest of the README: the project tree (`DcaShop.Web` "Razor views, layout + mini basket, home/error pages") and
  the Portal line ("the landing page") are still true. The story adds no route, REST resource or MCP tool.
- `GetProductSelectionQuery` / `GetProductSelectionResult` / `IGetProductSelectionInputPort` / `GetProductSelectionUseCase`:
  these are application names, not domain terms. The existing glossaries list no use cases either (`GetAllProducts`
  has no entry).
- `project/product.md`: `## Surfaces` already lists the home page on desktop and phone. The slider is a feature of an
  existing surface, not a new one.
- ADR: none. The placement decision (the slider in Product, composed by name like the mini basket) was answered by the
  domain contact (story `## Assumptions`, line 1). It is recorded in the story and the plan.
- Sync duty (AGENTS.md), not edited here because the Java twin follows as its own story (approved exception
  `scenario.home.slider-*`, `../dca-sample-specification/exceptions.md:7`, review 2026-10-31):
  - Java sample `dca-ecommerce-sample-java`: needs the `ProductSelection` value object, the selection use case, the
    homepage fragment with the same `data-test` selectors (`product-slider`, `product-slider-title`,
    `product-slider-card`, `-card-image`, `-card-name`, `-card-price`, `-card-link`, `product-slider-previous`,
    `product-slider-next`), `product-detail-title` / `product-detail-price` on the product page, the
    `.product-slider__*` rules copied into its `main.css`, the twelve `scenario.home.slider-*` browser tests, and the
    same glossary entries. After that, the README's temporary-difference sentence goes away.
  - `../planning/porting-status.md`: the Portal row (line 123) and the Product row would need a note once both samples
    carry the slider. Until then the .NET-first state is recorded by the exception.
- Gate observation (pipeline, not this repository's code): `check_proposals_landed` in
  `.agents/factory/story-gate.py:582-617` takes everything before the first `:` of a proposal line as the term,
  parenthetical included, so a glossary entry under the bare term (`ProductSelection`) does not satisfy it. Here the
  proposal labels are quoted verbatim in the table above. Candidate fix for dca-factory: strip a trailing
  parenthetical, or match the backticked identifier.
- Harness gap (AGENTS.md principle 1, carried over from the plan's `## Open assumptions`): the knowledge catalog has no
  node on composing one context's UI fragment into another context's page. Candidate: a recipe "compose a fragment into
  another context's page". Rule: none beyond the generic `DCA-HEX-007`/`DCA-HEX-011`. Marker: none. This is for the
  catalog's maintainers, not for this repository.
