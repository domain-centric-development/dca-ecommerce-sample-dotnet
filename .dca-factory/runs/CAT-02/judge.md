# Judge — CAT-02

Mode: **adopt** (`status: adopted`). Nothing was built, so the change under review is the claim that each
mapped test proves its scenario. Each test was read against its scenario's `Given` / `When` / `Then` / `And`,
and each break against what its test depends on. Nothing else was reviewed.

## Verdict
verdict: pass

## Perspectives covered
- ddd: not applied. The profile names `review-ddd`, but adopt mode reviews no production code, so there was no
  model to judge.
- hexagonal: not applied. The profile names `review-hexagonal`, but adopt mode reviews no production code.
- clean-code: in-session, limited to the new tests and the page-object addition. `review-clean-code` is named
  in the profile but was not invoked: adopt mode asks only whether the tests prove their scenarios.
- dca (added, `review.dca: dca-review`): not applied. There is no production change in adopt mode.
- Knowledge: the profile names `dca-knowledge`. It was not consulted, because adopting decides no pattern
  question.

## Confirmed defects
| Perspective | File:line | Severity | Defect | Fix |
| --- | --- | --- | --- | --- |
| — | — | — | none | — |

## Considered and dropped
- **The integration tests read markup as text** (`tests/DcaShop.IntegrationTests/ProductPageTest.cs:25-86`).
  This does not break the rule against end-user tests that read markup as text. These are HTTP-level
  integration tests, placed at that level on purpose by the plan. Only the happy path
  (`view-details-opens-the-product-page`) belongs in the browser, and that one drives Playwright
  (`tests/DcaShop.E2eTests/ProductPageE2eTest.cs:12-21`).
- **The browser break was not run and the patches were not checked with `git apply`** (tests.md, Notes).
  `breaks/…ViewDetailsOnACatalogueCardOpensThatProductsPage.patch` points every card at
  `Model.Products[0]`. The catalogue sorts ordinally by name (`InMemoryProductRepository.cs:53`), so the first
  card is `"Domain over Framework" Hoodie`, which starts with `"`. It is not "Domain-Driven Design", so the
  heading assertion at `ProductPageE2eTest.cs:20` would go red. That makes the break sound. Whether it applies
  and runs red is checked by the gate, not by this review.
- **Shared-scenario title** (`ProductPageE2eTest.cs:12`, DisplayName "View Details on a catalogue card opens
  that product's page"). AGENTS.md requires this title as a scenario in
  `../dca-sample-specification/scenarios.md`, plus a Java test with the same title. The specification is not
  among this stage's inputs, and `SharedScenariosTest` enforces the rule when the specification is supplied.
  The tests stage already flagged it for the user, who owns semantics. It is not repeated here as a finding.
- **`Catalog.cshtml:2` title change, `CatalogTitleE2eTest.cs`, `CatalogueTest.cs`, `ShopUnderTest.cs`,
  `E2eFactAttribute.cs`, `BaseE2eTest.cs`, AGENTS.md/README.md** in `story.diff`. These belong to the
  `catalogue-title` / `CAT-01` stories and to commit 5c2bb69's follow-up, which share this working tree.
  They are not part of CAT-02's claim, and adopt mode reviews nothing else. None of the CAT-02 tests depend on
  the catalogue's `<title>`: the back-link test asserts the `<h1>`.
- **`ProductPageE2eTest.cs:19` checks the path only as `/products/<guid>`, not as that product's id.** The
  heading assertion on the next line identifies the product, and the scenario's `Then` ("the product page of
  'Domain-Driven Design' opens") needs nothing more.

## Criteria re-checked
- view-details-opens-the-product-page: met. The test opens the catalogue (Given: seeded product) and follows
  "View Details" on the card titled exactly "Domain-Driven Design" (`ProductCatalogPage.cs:46-52`, When). It
  asserts that the product route opened and that the heading `product-detail-title` reads "Domain-Driven
  Design" (Then/And).
- product-page-shows-image-description-and-category: met. The test finds the page through the product's
  card. It asserts the image `src` `/images/products/ddd-book.webp` with alt "Domain-Driven Design", and that
  the image is served as 200 `image/*`. It asserts the description verbatim and the meta label "Category" with
  the value "Books" (`ProductPageTest.cs:25-45`). The break swaps the category value, and the test went red on
  the category assertion.
- product-page-title-is-the-product-name: met. `<title>` equals "Clean Architecture"
  (`ProductPageTest.cs:47-55`). The break fixes the title to "Product", and the test went red.
- product-page-breadcrumb: met. The joined link, separator and current-item texts equal "Home / Products /
  Clean Architecture". "Home" targets `/`, and "Products" targets `/products`, which renders "Our Products"
  (`ProductPageTest.cs:57-71`). Per the plan, what the home page shows is left to `CAT-04`, which is out of
  scope. The break retargets "Products", and the test went red.
- back-to-products-returns-to-the-catalogue: met. The test starts on the "Team Topologies" page and checks
  that the link reads "Back to Products". It follows the link and asserts the product grid and the `<h1>` "Our
  Products" (`ProductPageTest.cs:73-86`). The break retargets the link to `/`, and the test went red.
