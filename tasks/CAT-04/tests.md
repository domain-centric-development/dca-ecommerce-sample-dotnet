# Tests — CAT-04

Mode: **adopt**. The plan found no test for any of the seven scenarios at its planned level, so each one has a
characterization test: meant to be green on today's code, asserting the scenario's `Then` with its values, plus a
break under `breaks/`. No production code and no existing test was changed.

Carrier: done in this session with the `e2e-testing` craft (`carrier.test`) applied to the one browser test. The
profile names `knowledge: dca-knowledge`, but it was not consulted: adopting existing behaviour decides no pattern
question.

<!-- gate:tests -->
| criterion | test |
| --- | --- |
| home-page-welcomes-the-visitor | DcaShop.E2eTests.HomePageE2eTest#TheHomePageSaysWhatTheShopSells |
| home-page-title | DcaShop.IntegrationTests.HomePageTest#TheBrowserTabTitleIsTheShopName |
| browse-products-opens-the-catalogue | DcaShop.IntegrationTests.HomePageTest#BrowseProductsOpensTheCatalogue |
| view-cart-links-to-the-cart | DcaShop.IntegrationTests.HomePageTest#BelowTheHeadingViewCartLinksToTheCart |
| why-shop-with-us | DcaShop.IntegrationTests.HomePageTest#WhyShopWithUsShowsFourFeatures |
| popular-categories | DcaShop.IntegrationTests.HomePageTest#PopularCategoriesShowsFiveCategoriesAsTextNotLinks |
| shop-now-opens-the-catalogue | DcaShop.IntegrationTests.HomePageTest#ShopNowBelowTheCallToShopOpensTheCatalogue |

## Characterization
- DcaShop.E2eTests.HomePageE2eTest#TheHomePageSaysWhatTheShopSells
- DcaShop.IntegrationTests.HomePageTest#TheBrowserTabTitleIsTheShopName
- DcaShop.IntegrationTests.HomePageTest#BrowseProductsOpensTheCatalogue
- DcaShop.IntegrationTests.HomePageTest#BelowTheHeadingViewCartLinksToTheCart
- DcaShop.IntegrationTests.HomePageTest#WhyShopWithUsShowsFourFeatures
- DcaShop.IntegrationTests.HomePageTest#PopularCategoriesShowsFiveCategoriesAsTextNotLinks
- DcaShop.IntegrationTests.HomePageTest#ShopNowBelowTheCallToShopOpensTheCatalogue

## Files
- tests/DcaShop.E2eTests/HomePageE2eTest.cs
- tests/DcaShop.E2eTests/Pages/HomePage.cs
- tests/DcaShop.IntegrationTests/HomePageTest.cs
- tasks/CAT-04/breaks/DcaShop.E2eTests.HomePageE2eTest--TheHomePageSaysWhatTheShopSells.patch
- tasks/CAT-04/breaks/DcaShop.IntegrationTests.HomePageTest--TheBrowserTabTitleIsTheShopName.patch
- tasks/CAT-04/breaks/DcaShop.IntegrationTests.HomePageTest--BrowseProductsOpensTheCatalogue.patch
- tasks/CAT-04/breaks/DcaShop.IntegrationTests.HomePageTest--BelowTheHeadingViewCartLinksToTheCart.patch
- tasks/CAT-04/breaks/DcaShop.IntegrationTests.HomePageTest--WhyShopWithUsShowsFourFeatures.patch
- tasks/CAT-04/breaks/DcaShop.IntegrationTests.HomePageTest--PopularCategoriesShowsFiveCategoriesAsTextNotLinks.patch
- tasks/CAT-04/breaks/DcaShop.IntegrationTests.HomePageTest--ShopNowBelowTheCallToShopOpensTheCatalogue.patch

## Notes
- `HomePage` gains `HeadingAsync`, `SubtitleAsync` and `DescriptionAsync`. The hero's heading and paragraphs carry
  no `data-test` of their own, and adopt mode adds none to the markup. So they are found inside `data-test="hero"`
  by role (the level-1 heading) and by order (first and second `p`), not by CSS class. Existing page-object members
  are unchanged.
- The heading is read as the browser renders it (`InnerText`), so `domaincentric<span>.commerce</span>` reads as
  one sentence. The subtitle and the description are read by their wording (`TextContent`, whitespace collapsed):
  the stylesheet sets the subtitle in capitals, and the scenario states the wording, not the casing.
- `HomePageTest` mirrors `CatalogueTest`/`ProductPageTest`: `WebApplicationFactory<Program>`, `GET /`, regex
  extraction within the `data-test` section, HTML-decoded (`&#8364;` for €, `&amp;` for &). The two catalogue links
  are *followed* and the target must render `<h1>Our Products</h1>`. "View Cart" is checked only for its text and
  `href="/cart"` below the hero's `</h1>`, since the cart page is out of scope (`CRT-02`). The categories test
  asserts exactly five title/description pairs in order and no `<a` in the section.
- Breaks, all in `src/DcaShop.Web/Views/Home/Index.cshtml`, one element each:
  heading loses ".commerce"; tab title becomes "Home"; "Browse Products" and "Shop Now" point to `/`, where the page
  has no "Our Products" heading; "View Cart" points to `/products`; "Easy Returns" reads 14 instead of 30 days; a
  link inside the "Modeling" description, so the card no longer reads as text and the pair list has four entries.
- **Round 3, what was verified:** `dotnet` is on this session's PATH now. `dotnet build` succeeds with 0 warnings
  and 0 errors. The six `HomePageTest` integration tests pass. The browser test failed at first on the subtitle
  (`BOOKS, MODELLING …`, from CSS `text-transform`). The fix is in `HomePage.SubtitleAsync`/`DescriptionAsync`, which
  this stage added, and the test's assertions are unchanged. After the fix it passes. `dotnet format` has run.
- **Not verified here:** the breaks. Applying a patch to `Index.cshtml`, even `git apply --check`, needed
  approval that was not given. The gate applies them to a scratch copy.
- **Gate refusal (rounds 2 and 3):** `compiles` and all seven `tests-green` checks failed with `/bin/sh: dotnet:
  command not found`. The process that runs the gate has no .NET SDK on its PATH (`$HOME/.dotnet`, per AGENTS.md).
  That is environment, not something this stage may change (profile and runner configuration are off limits). The
  profile already passes `--logger trx`, so reports will exist once `dotnet` resolves.
- **Shared-scenarios rule:** the browser test's `DisplayName` is the scenario key in words, "Home page welcomes
  the visitor", because the story has no `Title:` lines. AGENTS.md requires that title in
  `../dca-sample-specification/scenarios.md` (`SharedScenariosTest`, only with `-p:SpecificationPath=…`), with a
  Java test under the same title. The specification was outside this stage's inputs. It still has to be added there.
  The user owns semantics.
- **Round limit (`rounds`):** escalated as decision CAT-04-01 and answered with option 2. The round count was reset
  (`tasks/CAT-04/.rounds` removed), and the test gate runs once more with nothing changed, so it checks the breaks
  too. This round changes no test, page object or break. Only this file changed: the open `## needs-human` section
  was removed.
- Unit tests: none. The plan names no domain invariant, and the story adopts page presentation only.
- Spikes: none written.
