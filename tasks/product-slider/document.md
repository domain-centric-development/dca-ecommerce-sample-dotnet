# Document — product-slider

Round: acceptance correction product-slider-accept-1 (8 cards; 4 in view on xl and l, 2 on m, 1 on s). Carrier for
the glossary: `ubiquitous-language` (`carrier.glossary` in `.agents/factory/factory.profile.yaml`), applied in-session
in the glossary's existing format. Carrier for the designed map: `context-map` (`carrier.domain`), not invoked — the
correction changes no relationship. The profile names the knowledge source `dca-knowledge`; this stage took no
architecture decision and did not consult it.

## Glossary
| Term | Context | Added or changed | Definition source |
| --- | --- | --- | --- |
| ProductSelection | Product | changed: `src/DcaShop.Product/Domain/glossary.md:109`, "Up to four" → "Up to eight" different products | plan `## Glossary proposals` ("the document stage changes it to eight"); checked against `src/DcaShop.Product/Domain/Model/ProductSelection.cs:10` (`MaxSize = 8`) and `:30` (`Math.Min(MaxSize, pool.Length)`) |

## Documents updated
| File | What changed | Verified by |
| --- | --- | --- |
| src/DcaShop.Product/Domain/glossary.md | `ProductSelection` definition names eight products | read `src/DcaShop.Product/Domain/Model/ProductSelection.cs:7-30` |

## Not documented
- `README.md:156-158`: unchanged and still true. It names the slider (`data-test="product-slider"`) and "the
  `.product-slider__*` rules in `main.css`" as the temporary .NET-only difference; the new `@media (max-width: 768px)`
  rule (`src/DcaShop.Web/wwwroot/css/main.css:2915-2919`) is a `.product-slider__card` rule and so falls under that
  sentence. The README states no card count. Verified: `grep -n -i -E "slider|four|768" README.md` (only lines 156-157).
  The divergence is covered by the approved exception `scenario.home.slider-*`
  (`../dca-sample-specification/exceptions.md:7`), not cited in the README because reader artifacts do not link across
  repositories.
- `src/DcaShop.Portal/Domain/glossary.md:52` ("Product slider"): names no count and no cards-per-view; still true.
- `project/product.md`, `project/backlog/homepage-discovery/epic.md`: no slider count stated (grep `slider|four`;
  `project/product.md:37` "four sizes" is about page sizes, unrelated). The story file already says eight.
- `project/domain.md` and `docs/architecture/context-map.md`: no context or relationship changed; `git diff --quiet docs/`
  is clean.
- Cards-per-view per size (4 / 4 / 2 / 1): a presentation detail held by `main.css` and the browser tests
  (`tests/DcaShop.E2eTests/HomeSliderTabletE2eTest.cs`, `HomeSliderSmallTabletE2eTest.cs`), not a domain term; no
  reader document restates it.
- Sync duty (AGENTS.md): the Java sample follows as its own story (story `## Out of scope`, exception review date
  2026-10-31). It then needs `MaxSize` 8, the size-m `.product-slider__card` rule in its `main.css`, the sixteen
  `scenario.home.slider-*` tests and the "Up to eight" glossary wording; `../planning/porting-status.md` gets its
  note once both samples carry the slider.
- Harness answer (AGENTS.md principle 1) for this correction: catalog node none, rule none, marker none — a constant
  and a stylesheet breakpoint carry no architecture.
