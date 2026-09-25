# Tidy — product-slider

Carrier: in-session; the profile names no `carrier.tidy`. Guard `dca-discipline` (`carrier.guard`) kept as the
stage describes it: nothing was edited, so no invariant was put at risk.

## Moves
| File | Move | Why it reads better |
| --- | --- | --- |

No move. The build left the story's code small and already in the project's own shape: `ProductSelection` is a
single-purpose value with one factory (`Draw`) and a named `MaxSize`; `GetProductSelectionUseCase` reads top to
bottom at one level of abstraction (catalog → priced ids → draw → enrich); `ProductSliderViewComponent` maps to its
view model inline exactly like its sibling `ProductPageController`; the view model carries primitives only. There is
no helper the story made redundant and no duplicate it introduced. A move made only to have made one would add
noise to the judge's diff.

## Left alone
- `ProductSliderViewComponent` / `ProductPageController` both map `EnrichedProduct` to primitives with
  `p.CurrentPrice.ToString()` inline: the same one-line projection per view model, each with a different shape; lifting
  a shared mapper would touch `ProductPageController`, which is outside the footprint and not a real duplicate.
- `ProductContextRegistration` registers the use case through a factory lambda to pass `Random.Shared`: registering
  `Random` as a service instead would change the container graph for every consumer — a design question, not tidying.
- `GetProductSelectionUseCase` builds `byId` from the catalog after the draw: at the catalog's size a second pass is
  immaterial, and folding it into the candidate filter would trade clarity for nothing.
- Inline paging script in `Views/Shared/Components/ProductSlider/Default.cshtml`: the project has no bundled
  script pipeline for components; moving it to `wwwroot/js/` would be a structural choice for the plan, and the
  deviation recorded in `build.md` (controls before the track) is behaviour the keyboard test depends on.
- `DcaShop.UnitTests.Cart.ActiveCartUniquenessTest.ConcurrentRequestsShareOneCart` failed once during the second
  gate run (while the shop was running beside it for the E2E suite) and passed in six isolated reruns and in the
  stage's own run: a timing-sensitive concurrency test in the Cart context, outside this story's footprint and
  untouched by it. A finding for whoever owns that test, not an edit here.

## Checks
- dotnet build: 0 errors
- dotnet test tests/DcaShop.UnitTests --logger trx: 176 passed, 4 skipped (specification tests without `SpecificationPath`)
- dotnet test tests/DcaShop.IntegrationTests --logger trx: 58 passed, 1 skipped (pre-existing)
- E2E_BASE_URL=http://localhost:5080 dotnet test tests/DcaShop.E2eTests --logger trx: 33 passed, 1 skipped (pre-existing)
- dotnet test tests/DcaShop.ArchitectureTests: 129 passed
- dotnet format --verify-no-changes: clean (formatFix not run — nothing was edited)
