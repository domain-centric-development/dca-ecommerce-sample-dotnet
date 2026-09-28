# Judge — CAT-01

## Verdict
verdict: pass

## Perspectives covered
Adopt mode (`status: adopted`): the change is a claim, not code. The review reads every mapped test against its
scenario and nothing else, because nothing was built. Done in this session.
- ddd: not run. Adopt mode, and CAT-01 built no production code to review (carrier `review-ddd` is available).
- hexagonal: not run, for the same reason (carrier `review-hexagonal` is available).
- clean-code: not run, for the same reason (carrier `review-clean-code` is available).
- dca (added): not run, for the same reason (carrier `dca-review` is available).
- scenario-to-test proof (adopt mode): in-session.
- Knowledge: the profile names `dca-knowledge`, but I did not consult it. An adoption decides no pattern question.

## Confirmed defects
| Perspective | File:line | Severity | Defect | Fix |
| --- | --- | --- | --- | --- |
| — | — | — | none | — |

## Considered and dropped
- **The integration tests parse markup as text with regexes, although the profile names `browser: playwright`.**
  Not a defect. That rule is about *end-user* tests. `CatalogueTest` is an integration test at the level the plan
  set for these four scenarios (`WebApplicationFactory<Program>`, real HTTP pipeline). The one browser test is the
  story's happy path, `SeededCatalogE2eTest`.
- **`SeededCatalogE2eTest.cs:20` counts `product-card-title` elements, not `product-card` elements.** Not a defect.
  `Catalog.cshtml:26` renders exactly one title inside every card, with no condition, so the title count is the
  card count.
- **The Given "seeded … of `seed-catalogue.json`": the seed lives inline in `SampleDataSeeder`, not in that
  file.** Not a defect. The vector belongs to the unpublished specification and is not in the build (AGENTS.md).
  The tests pin its observable result (21 cards, the named first product, the "Domain-Driven Design" card data).
  The plan records this as an open assumption.
- **`catalogue-page-heading` passes only because of the uncommitted `catalogue-title` change at
  `Catalog.cshtml:2`.** Not a defect of CAT-01's proof. The story declares `depends_on: [catalogue-title]`, and
  the break for this test reverts exactly that line and turns the test red on the title assertion (tests.md). The
  catch is order: CAT-01 can be delivered only after `catalogue-title` is. That story is currently stopped.
- **Changes in the diff outside CAT-01** (`BaseE2eTest.cs`, `ShopUnderTest.cs`, `E2eFactAttribute.cs`, the csproj,
  README/AGENTS.md from commit 5c2bb69, which decision CAT-01-02 accepted; `ProductPageTest.cs`,
  `ProductPageE2eTest.cs` and `ProductCatalogPage.ViewProductAsync`, which belong to CAT-02; `CatalogTitleE2eTest.cs`
  and the page object's title/breadcrumb helpers, which belong to `catalogue-title`). Not reviewed: adopt mode
  reviews only the tests mapped to CAT-01's scenarios.
- **`EveryCardOffersViewDetailsLeadingToItsOwnProduct` asserts only `NotEmpty`, not 21 cards**
  (`CatalogueTest.cs`, diff line 393). Not a defect. The scenario asks about *every* card, not how many there are.
  `catalogue-lists-the-seeded-range` pins the count.

## Criteria re-checked
- catalogue-lists-the-seeded-range: met. `SeededCatalogE2eTest.CatalogListsTheSeededProductsInNameOrder` opens
  `/products` in the browser against the seeded shop and asserts `21` cards (line 20).
- catalogue-is-in-name-order: met. `CatalogueTest.TheCardsAreInOrdinalOrderOfTheProductNameStartingWithTheEnamelPin`
  asserts that the card titles equal their own `StringComparer.Ordinal` order and that `titles[0]` is
  `"Bounded Context" Enamel Pin`. Its break (a reordered repository) turns it red on the order assertion.
- card-shows-name-description-and-image: met. `CatalogueTest.TheCardOfDomainDrivenDesignShowsItsNameDescriptionAndImage`
  - finds the single card titled "Domain-Driven Design"
  - asserts its full seeded description
  - asserts the image `src` `/images/products/ddd-book.webp` with alt "Domain-Driven Design", and that the image
    is served as `image/*` with status 200
- card-leads-to-the-product: met. `CatalogueTest.EveryCardOffersViewDetailsLeadingToItsOwnProduct` checks, for every
  card:
  - the link reads "View Details"
  - following it opens a product page whose `product-detail-title` is that card's name
- catalogue-page-heading: met, on the working tree that carries `catalogue-title`.
  `CatalogueTest.TheCatalogueIsTitledProductCatalogWithHeadingOurProductsBelowHomeProducts` asserts:
  - `<title>` "Product Catalog"
  - `<h1>` "Our Products"
  - breadcrumb "Home / Products"

  It holds in a delivered tree only once `catalogue-title` is delivered (`depends_on`).
