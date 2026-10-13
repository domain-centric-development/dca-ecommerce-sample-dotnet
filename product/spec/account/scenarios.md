# account

### Rule: Out of the box, a page on the shop's own host may frame the shop and use it

#### a-page-on-another-port-embeds-the-shop
- Given the shop reached as `localhost` and a page served from another port of `localhost` that frames the catalogue
- When the visitor opens the first product inside the frame and presses "Add to Cart"
- Then the framed shop's cart page shows the product
Test: DcaShop.E2eTests.EmbeddedShopE2eTest#ASlideDeckOnAnotherPortFramesTheShopAndAddsToTheCart

### Rule: Out of the box, a page on another site cannot use the framed shop

#### a-foreign-site-cannot-add-to-the-cart
- Given the shop reached as `localhost` and a page on `127.0.0.1` that frames the catalogue
- When the visitor opens the first product inside the frame and presses "Add to Cart"
- Then the product does not reach the framed shop's cart
Test: DcaShop.E2eTests.EmbeddingPolicyE2eTest#APageOnAnotherSiteCannotPutAProductIntoTheFramedShopsCart

### Rule: A shop started for cross-site embedding works in a foreign site's frame

#### a-shop-started-for-embedding-works-cross-site
- Given a shop started for cross-site embedding, so that its cookies are sent as `SameSite=None; Secure`, reached as `localhost`, and a page on `127.0.0.1` that frames the catalogue
- When the visitor opens the first product inside the frame and presses "Add to Cart"
- Then the framed shop's cart page shows the product
Test: DcaShop.E2eTests.EmbeddingPolicyE2eTest#AShopStartedForEmbeddingTakesTheProductIntoItsCartInsideAForeignSitesFrame

### Rule: A shop started with framing switched off cannot be framed by any other page

#### a-shop-started-without-framing-refuses-the-frame
- Given a shop started with framing switched off and a page on another port of the shop's host that frames the catalogue
- When the visitor opens that page
- Then the shop's pages answer with `X-Frame-Options: SAMEORIGIN` and the browser shows nothing of the shop inside the frame
Test: DcaShop.E2eTests.EmbeddingPolicyE2eTest#AShopStartedWithFramingSwitchedOffShowsNothingInsideAnotherPagesFrame
