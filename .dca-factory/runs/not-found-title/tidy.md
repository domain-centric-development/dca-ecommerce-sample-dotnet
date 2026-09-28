# Tidy — not-found-title

Done in this session. The profile names no `carrier.tidy`. I applied the guard `carrier.guard: dca-discipline`, but this
stage wrote no code file, only this one. The story's one production file is a Razor view in the web host, which sits
outside every DCA layer.

## Moves
| File | Move | Why it reads better |
| --- | --- | --- |

None. The story's production change is one deleted line in `src/DcaShop.Web/Views/Error/404.cshtml`
(`@{ ViewData["Title"] = "Page Not Found"; }`). The rest of the view is markup from before this story, so this story
left nothing there to tidy. The other two files in the footprint (`tests/DcaShop.E2eTests/NotFoundTitleE2eTest.cs`,
`tests/DcaShop.E2eTests/Pages/NotFoundPage.cs`) are tests, and this stage does not change tests.

## Left alone
- `404.cshtml`, the body markup: every element sits at column 0, as it did before this story. Reindenting would
  reformat lines this story never touched. The story's answered assumption (CAT-03-01) also keeps the body as it is.
- `NotFoundPage.NavigateToAsync` waits on `error-browse-link` because the error page's container has no `data-test`.
  Adding one would be a view change that no criterion asks for. It is a note for a later story, not a tidy move.
- `NotFoundPage.DocumentTitleAsync` duplicates `ProductCatalogPage.DocumentTitleAsync` (`Page.TitleAsync()`). Lifting
  it into `BasePage` would edit test code, and `BasePage.cs` also has uncommitted changes from other stories. This is
  a finding for the judge, not a change.

## Checks
- dotnet build: 0 errors
- dotnet test tests/DcaShop.UnitTests: 178 passed, 4 skipped
- dotnet test tests/DcaShop.IntegrationTests: 83 passed, 1 skipped
- dotnet test tests/DcaShop.ArchitectureTests: 129 passed
- dotnet test tests/DcaShop.E2eTests: 43 passed, 1 skipped
- dotnet format --verify-no-changes: clean. `formatFix` was not run because this stage changed no code.
