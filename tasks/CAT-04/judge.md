# Judge — CAT-04

Mode: **adopt** (`status: adopted`). Nothing was built, so the claim under review is that each mapped test
proves its scenario, and that each break changes something the test depends on. The inputs were the story,
`plan.md`, `tests.md` and `.verify/story.diff`. There is no `build.md`, since an adopted story has no build
stage, and no `.judge-previous.md`, since this is the first round. Beyond those, this stage read the seven
patches under `breaks/`, the gate report `.verify/gate-test.103510.txt`, and the headings of the shared
specification's `scenarios.md`, for the one finding that needs it.

## Verdict
verdict: pass

## Perspectives covered
- ddd: in-session, adopt mode. `review-ddd` is available, but the change contains no model code, so only the
  scenario-to-test reading applies.
- hexagonal: in-session, adopt mode. `review-hexagonal` is available, but no port, adapter or dependency changed.
- clean-code: in-session, adopt mode. `review-clean-code` is available. The only code is test code, read against
  the scenarios.
- dca (added, `review.dca: dca-review`): not run. In adopt mode nothing else is reviewed, because no production
  code changed.
- Knowledge: the profile names `dca-knowledge`. It was not consulted, because no finding turns on a pattern or
  rule question.

## Confirmed defects
| Perspective | File:line | Severity | Defect | Fix |
| --- | --- | --- | --- | --- |
| scenario/spec | `tests/DcaShop.E2eTests/HomePageE2eTest.cs:14` | minor | The browser test's `DisplayName` "Home page welcomes the visitor" has no scenario in the shared specification: `scenarios.md` has no home-page heading. With `-p:SpecificationPath=…` set, `SharedScenariosTest` fails on "a test without a scenario". The default build and CI run without that switch and stay green, and adopt mode reviews only the test-to-scenario claim, so this does not block. | The user owns semantics. Add a `scenario.portal.home-page-welcomes-the-visitor` (or similar) with this exact title to `dca-sample-specification/scenarios.md`, plus the Java suite's test under the same title. The test stage already recorded this in `tests.md` Notes. |

## Considered and dropped
- The integration tests read markup as text (`HomePageTest.cs`). This is not the major "end-user test reads
  markup" finding. These are the HTTP-level integration tests the plan chose for six non-happy-path scenarios. The
  one end-user test drives Playwright.
- Test levels: the only browser test covers the happy path (`home-page-welcomes-the-visitor`, marked in the
  story). The other six are integration tests, as the plan says. No adapter changed and no port is mocked, so the
  level rules are met.
- `SubtitleAsync`/`DescriptionAsync` read `TextContent`, not the rendered `InnerText`
  (`tests/DcaShop.E2eTests/Pages/HomePage.cs:83-90`). This is deliberate: CSS `text-transform` capitalises the
  subtitle, and the scenario states wording, not casing. The heading uses `InnerText`, so the `.commerce` span
  counts as part of the sentence.
- The regex `Link` helper requires `href` before `data-test` (`HomePageTest.cs:130`). That is brittle to attribute
  order but correct for today's markup, and a reordering fails loudly (`Assert.True(match.Success…)`), never
  silently. It is a preference, not a defect.
- The gate report has no line showing the breaks ran red (`gate-test.103510.txt`), and the test stage could not
  apply them locally. Each patch was read instead (see below). Every one alters exactly what its test asserts.
  Executing breaks is the gate's job, not a defect in the change.
- The plan's out-of-scope note: the story's "A product slider on the home page: not part of the shop today"
  contradicts `Index.cshtml:11`. This change delivers nothing new for the slider and no test here asserts it. It is
  a backlog wording issue, already routed to `/factory-backlog` in the plan. It is not a defect of this change.
- Product and tech description: no surface, state, framework or integration was added.

## Criteria re-checked
- home-page-welcomes-the-visitor: met. `HomePageE2eTest.cs:17-24` opens `/` in the browser and asserts the heading,
  the subtitle and the description with the scenario's exact values. The break removes `.commerce` from the h1,
  and the heading assertion fails.
- home-page-title: met. `HomePageTest.cs:22-29` asserts `<title>` is "domaincentric.commerce". The break sets the
  title to "Home".
- browse-products-opens-the-catalogue: met. `HomePageTest.cs:32-42` reads the "Browse Products" link, follows its
  target and asserts `<h1>Our Products</h1>`. The break points it to `/`, whose h1 carries a class and other text,
  so the match fails.
- view-cart-links-to-the-cart: met. `HomePageTest.cs:45-56` looks only in the hero after `</h1>` and asserts the
  text "View Cart" and `href="/cart"`. The break sets the href to `/products`.
- why-shop-with-us: met. `HomePageTest.cs:59-81` asserts the section title and exactly four title/description
  pairs, in order, HTML-decoded (so the € is checked). The break changes 30 days to 14.
- popular-categories: met. `HomePageTest.cs:84-108` asserts the title and exactly five pairs in order (so `&` is
  checked), and no `<a` in the section, which covers "as text and not as links". The break puts a link in the
  Modeling description, which breaks both the pair list and the no-link assertion.
- shop-now-opens-the-catalogue: met. `HomePageTest.cs:111-126` asserts the Given ("Ready to Draw Some
  Boundaries?" and the subtitle, both inside `cta-section`), follows "Shop Now" and asserts "Our Products". The
  break points the link to `/`.
