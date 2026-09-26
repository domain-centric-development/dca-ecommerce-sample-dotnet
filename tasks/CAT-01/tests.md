# Tests — CAT-01

Mode: **adopt**. The happy path already has its test at the planned level (`SeededCatalogE2eTest`, browser). For
the other four scenarios the plan found no test at the planned integration level. Each one now has a
characterization test in `tests/DcaShop.IntegrationTests/CatalogueTest.cs`: green on today's code, asserting the
scenario's `Then` with its values, plus a break under `breaks/`. No production code and no existing test was
changed.

Carrier: done in this session. `carrier.test` is `e2e-testing`, but this stage wrote no browser test, so its craft
did not apply. The profile names `knowledge: dca-knowledge`, but it was not consulted: adopting existing behaviour
decides no pattern question.

<!-- gate:tests -->
| criterion | test |
| --- | --- |
| catalogue-lists-the-seeded-range | DcaShop.E2eTests.SeededCatalogE2eTest#CatalogListsTheSeededProductsInNameOrder |
| catalogue-is-in-name-order | DcaShop.IntegrationTests.CatalogueTest#TheCardsAreInOrdinalOrderOfTheProductNameStartingWithTheEnamelPin |
| card-shows-name-description-and-image | DcaShop.IntegrationTests.CatalogueTest#TheCardOfDomainDrivenDesignShowsItsNameDescriptionAndImage |
| card-leads-to-the-product | DcaShop.IntegrationTests.CatalogueTest#EveryCardOffersViewDetailsLeadingToItsOwnProduct |
| catalogue-page-heading | DcaShop.IntegrationTests.CatalogueTest#TheCatalogueIsTitledProductCatalogWithHeadingOurProductsBelowHomeProducts |

## Characterization
- DcaShop.IntegrationTests.CatalogueTest#TheCardsAreInOrdinalOrderOfTheProductNameStartingWithTheEnamelPin
- DcaShop.IntegrationTests.CatalogueTest#TheCardOfDomainDrivenDesignShowsItsNameDescriptionAndImage
- DcaShop.IntegrationTests.CatalogueTest#EveryCardOffersViewDetailsLeadingToItsOwnProduct
- DcaShop.IntegrationTests.CatalogueTest#TheCatalogueIsTitledProductCatalogWithHeadingOurProductsBelowHomeProducts

## Files
- tests/DcaShop.IntegrationTests/CatalogueTest.cs
- tasks/CAT-01/breaks/DcaShop.IntegrationTests.CatalogueTest--TheCardsAreInOrdinalOrderOfTheProductNameStartingWithTheEnamelPin.patch
- tasks/CAT-01/breaks/DcaShop.IntegrationTests.CatalogueTest--TheCardOfDomainDrivenDesignShowsItsNameDescriptionAndImage.patch
- tasks/CAT-01/breaks/DcaShop.IntegrationTests.CatalogueTest--EveryCardOffersViewDetailsLeadingToItsOwnProduct.patch
- tasks/CAT-01/breaks/DcaShop.IntegrationTests.CatalogueTest--TheCatalogueIsTitledProductCatalogWithHeadingOurProductsBelowHomeProducts.patch

## Notes
- `tests/DcaShop.E2eTests/BaseE2eTest.cs` differs from the story's test baseline. Commit 5c2bb69 changed it
  outside this story, not this stage, and this stage leaves it as HEAD holds it. Decision CAT-01-02 accepted
  that change for CAT-01 (answered 2026-09-26T06:40:42Z by Christoph Bloemer).
- `CatalogueTest` is a new class. The existing `ProductPageTest` belongs to CAT-02 (it is uncommitted there), so
  it was left untouched. The new class copies that class's `WebApplicationFactory<Program>` pattern and its
  HTML-parsing helpers.
- Order: the card titles of `GET /products` must equal their own ordinal sort, and the first must be
  `"Bounded Context" Enamel Pin`.
- Card: the "Domain-Driven Design" card shows its full seeded description in `product-card__description`, and an
  `<img>` with `src` `/images/products/ddd-book.webp` and `alt` "Domain-Driven Design". The image is served with
  status 200 and an `image/*` content type.
- Link: every card's `view-product` link must read "View Details", and following it must open a product page whose
  `product-detail-title` is that card's name. All 21 cards are followed.
- Title: `<title>` is "Product Catalog", `<h1>` is "Our Products" and the breadcrumb texts read "Home / Products".
  Today's code passes this only because of the uncommitted `catalogue-title` change to `Catalog.cshtml` line 2.
  The plan says CAT-01 depends on that story. Its break reverts exactly that line to "Products".
- Verified in this session: all four tests are green on the working tree. I then made the four break changes by
  hand with the same edits the patches contain, all at once, since they touch separate lines. Each test was red
  **on its own assertion**:
  - order: collections differ, "Wooden Hexagon Desk Model" first
  - description: `""` vs the seeded text
  - link text: "Details" vs "View Details"
  - title: "Products" vs "Product Catalog"

  Afterwards the edits were reverted, and `git diff src/` shows only the `catalogue-title` line again.
  `git apply --check` on the patch files could not be run in this session (it needs approval). The hunks were
  written from the working tree's current lines.
- Unit tests: none added. The plan names no new invariant. The order rule is already covered at unit level by
  `InMemoryProductRepositoryTest.FindAllIsOrderedByProductNameWhateverTheOrderTheyWereSavedIn`.
- `dotnet format` was run on the new test file.
