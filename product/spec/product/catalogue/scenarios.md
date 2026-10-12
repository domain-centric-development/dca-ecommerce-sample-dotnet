# product — catalogue

### Rule: The shop starts with its sample catalogue

#### catalogue-lists-the-seeded-range
- Given the shop has started and seeded the sample catalogue of `seed-catalogue.json`
- When a visitor opens the catalogue page
- Then it shows 21 product cards
Test: DcaShop.E2eTests.SeededCatalogE2eTest#CatalogListsTheSeededProductsInNameOrder

### Rule: Products are listed by name, compared character by character

#### catalogue-is-in-name-order
- Given the seeded sample catalogue
- When a visitor opens the catalogue page
- Then the card titles are in ordinal order of the product name
- And the first card is `"Bounded Context" Enamel Pin`
Test: DcaShop.IntegrationTests.CatalogueTest#TheCardsAreInOrdinalOrderOfTheProductNameStartingWithTheEnamelPin

### Rule: A card shows what the product is

#### card-shows-name-description-and-image
- Given the seeded product "Domain-Driven Design"
- When a visitor opens the catalogue page
- Then its card shows the name "Domain-Driven Design", its description and its image
Test: DcaShop.IntegrationTests.CatalogueTest#TheCardOfDomainDrivenDesignShowsItsNameDescriptionAndImage

#### card-leads-to-the-product
- Given the seeded sample catalogue
- When a visitor opens the catalogue page
- Then every card offers a "View Details" link to that product's page
Test: DcaShop.IntegrationTests.CatalogueTest#EveryCardOffersViewDetailsLeadingToItsOwnProduct

### Rule: The page is recognisable as the catalogue

#### catalogue-page-heading
- Given the seeded sample catalogue
- When a visitor opens the catalogue page
- Then the page title is "Product Catalog", the heading reads "Our Products" and the breadcrumb reads "Home / Products"
Test: DcaShop.IntegrationTests.CatalogueTest#TheCatalogueIsTitledProductCatalogWithHeadingOurProductsBelowHomeProducts

### Rule: The catalogue page carries the catalogue's name as its title

#### catalogue-tab-reads-product-catalog
Title: The catalogue page is titled Product Catalog
- Given the seeded sample catalogue
- When a visitor opens the catalogue page
- Then the page title is "Product Catalog"
Test: DcaShop.E2eTests.CatalogTitleE2eTest#CatalogPageIsTitledProductCatalog
