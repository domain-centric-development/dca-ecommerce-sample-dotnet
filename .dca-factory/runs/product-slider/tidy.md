# Tidy — product-slider

Round: acceptance correction product-slider-accept-1. Carrier: in-session; the profile names no `carrier.tidy`.
Guard `dca-discipline` (`carrier.guard`) kept as the stage describes it: nothing was edited, so no invariant was put
at risk.

## Moves
| File | Move | Why it reads better |
| --- | --- | --- |

No move. This round changed two things, both already in their plainest form: `ProductSelection.MaxSize` 4 → 8 (one
named constant, which the summary and `Draw`'s doc reference through `<see cref="MaxSize"/>` rather than a number, so
no comment went stale) and one `@media (max-width: 768px)` rule for `.product-slider__card`, placed before the
existing 480 px rule, using the same `--product-slider-gap` variable and the same `calc` shape as the default
four-per-view rule. No helper was made redundant and no duplicate introduced. The rest of the footprint (use case,
view component, view model, registration, views) is unchanged since the first delivery, whose tidy stage found it
clean; nothing in this round changes that reading.

## Left alone
- `src/DcaShop.Product/Domain/glossary.md` still defines the product selection as "Up to four different products":
  a glossary entry is the document stage's item (build.md names it), and tidy renames nothing a glossary names.
- `main.css` now diverges from the Java sample's copy by the size-m rule: a sync note for the document stage and the
  Java twin's own story (story `## Out of scope`), not a tidy edit.
- The findings of the first delivery's tidy stage still hold and remain untouched: the inline `EnrichedProduct`
  projection in `ProductSliderViewComponent` beside `ProductPageController`, the `Random.Shared` factory lambda in
  `ProductContextRegistration`, the second pass building `byId` in `GetProductSelectionUseCase`, and the inline
  paging script in `Views/Shared/Components/ProductSlider/Default.cshtml`.

## Checks
- dotnet build: 0 errors
- dotnet format --verify-no-changes: clean (formatFix not run — nothing was edited)
- `E2E_BASE_URL=http://localhost:5080 python3 .agents/factory/story-gate.py --story product-slider --stage tidy`
  (shop started with `dotnet run --project src/DcaShop.Web`, stopped afterwards): `gate:pass story product-slider
  stage tidy` — compiles, all 18 criterion tests green, red-proof held, unit suite 156 cases and integration suite
  55 cases with none failed, architecture suite and format passed, files-listed 0 of 0
