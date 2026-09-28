# Tidy — catalogue-title

## Moves
| File | Move | Why it reads better |
| --- | --- | --- |

No moves. The story's only production change is one line, `ViewData["Title"] = "Product Catalog"` in
`src/DcaShop.Web/Views/Product/Catalog.cshtml:2`. It is already as simple as it can be and uses the story's own words.
There is no name to fix, nothing to split, no duplicate and no comment. The build stage left nothing redundant. The
other three files in `.verify/changed.txt` are test code, which this stage may not change:
`tests/DcaShop.E2eTests/CatalogTitleE2eTest.cs`, `tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs` and
`tests/DcaShop.IntegrationTests/CatalogueTest.cs`.

## Left alone
- `ProductCatalogPage.ViewProductAsync(string name)` and its `System.Text.RegularExpressions` import: these are
  uncommitted changes from another story (the plan says so). They are outside this story's footprint, and they are
  test code.
- The breadcrumb's current segment `Products` and the heading `<h1>Our Products</h1>` no longer match the tab title
  "Product Catalog". The story's assumption (CAT-01-01) says this is intended, so it is not a tidy question.
- The E2E test's `DisplayName` has to be a scenario title in `../dca-sample-specification/scenarios.md`, and the Java
  suite has to match it (sync duty, AGENTS.md). Both are outside this stage's inputs and were not checked. This is a
  note for the judge and document stages.

## Checks
- `dotnet build`: 0 errors
- `dotnet test tests/DcaShop.UnitTests`: passed, 176 passed, 4 skipped
- `dotnet test tests/DcaShop.IntegrationTests`: passed, 67 passed, 1 skipped
- `dotnet test tests/DcaShop.ArchitectureTests`: passed, 129 of 129
- `dotnet test tests/DcaShop.E2eTests`: passed, 39 passed, 1 skipped (the suite started the shop itself)
- `dotnet format --verify-no-changes`: clean
- `dotnet format` (formatFix): run last. It changed no file; `git status` of `src/` and `tests/` is the same as
  before.

## Carriers
- `carrier.tidy`: not named in the profile, so the stage was done in-session.
- `carrier.guard: dca-discipline`: applied in-session to the one file this stage wrote, this `tidy.md`. It contains no
  code, so no invariant is at stake: the domain stays framework-free, no port or adapter changed, and no
  cross-context reference or event was added.
