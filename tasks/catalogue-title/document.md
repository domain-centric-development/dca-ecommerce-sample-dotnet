# Document — catalogue-title

## Glossary
| Term | Context | Added or changed | Definition source |
| --- | --- | --- | --- |
| — | Product | nothing added or changed | `tasks/catalogue-title/plan.md` has no glossary proposal; `tasks/catalogue-title/.verify/story.diff` introduces no domain identifier. The one production change is a page-title string in `src/DcaShop.Web/Views/Product/Catalog.cshtml:2`. The new names `CatalogTitleE2eTest`, `DocumentTitleAsync` and `BreadcrumbAsync` are test support, not domain language. `src/DcaShop.Product/Domain/glossary.md` was read and needs no change. |

## Documents updated
| File | What changed | Verified by |
| --- | --- | --- |
| — | nothing had to change | Searched `*.md` in the repository for `Product Catalog`, `"Products"`, `page title` and `<title`. `README.md:7,92`, `project/domain.md:21` and `docs/architecture/context-map.md:27,40` use "Product Catalog" as the name of the bounded context, not as the catalogue page's tab title, and they are still correct. No reader document names the catalogue page's document title. The story changes no relationship between contexts (`plan.md`, Context), so neither `project/domain.md` (`carrier.domain: context-map`) nor the generated `docs/architecture/context-map.md` changes. The architecture suite regenerates that map and passed 129 of 129 (`build.md`, Checks). `git status` shows the generated map unmodified. |

## Not documented
- Scenario sync: the browser test's `DisplayName` "The catalogue page is titled \"Product Catalog\"" (`tests/DcaShop.E2eTests/CatalogTitleE2eTest.cs:15`) has to be a scenario title in the scenarios file of the sample specification, a separate repository outside this checkout, and the Java suite needs a test with the same title (`SharedScenariosTest`, AGENTS.md "Shared semantics since WP-39"). The specification and the Java sample are outside this stage's inputs, so this was not checked. It waits for a human or a sync step in the specification repository. Decision CAT-01-01 records that the Java shop already titles the catalogue "Product Catalog".
- `tests/DcaShop.IntegrationTests/CatalogueTest.cs` appears in `tasks/catalogue-title/.verify/changed.txt`, but it belongs to the adopted story CAT-01 (`judge.md`, "Considered and dropped"). It is not documented here.
- `knowledge: dca-knowledge` was not consulted, because no architecture question came up. `carrier.glossary: ubiquitous-language` was not run, because no term was entered.
