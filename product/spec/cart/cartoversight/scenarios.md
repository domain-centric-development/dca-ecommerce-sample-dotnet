# cart — cartoversight

### Rule: Only staff list every cart; a stranger is challenged, a customer is forbidden

#### staff-list-every-cart
- Given Ada's cart holds "Domain-Driven Design" × 2 and a client holding a staff token
- When it sends `GET /api/carts`
- Then the shop answers 200 with `carts`, one of which has Ada's `cartId`, her `customerId`, `status` "ACTIVE", `itemCount` 1, `total` 109.98 and `currency` "EUR"
Test: DcaShop.IntegrationTests.CartApiTest#TheStaffListSpellsACartsStatusAsTheSingleCartsAnswerDoes

#### a-stranger-cannot-list-carts
- Given a client without a token
- When it sends `GET /api/carts`
- Then the shop answers 401 with the header `WWW-Authenticate: Bearer`
Test: DcaShop.IntegrationTests.CartApiTest#ListingCartsWithoutATokenIsChallengedForABearerToken

#### a-customer-cannot-list-carts
- Given a client holding the customer token of Ada
- When it sends `GET /api/carts`
- Then the shop answers 403
Test: DcaShop.IntegrationTests.ApiFlowTest#ListingEveryCartNeedsStaff

### Rule: The cart oversight totals each cart at today's prices

#### the-staff-list-totals-at-todays-prices
- Given Ada's cart holds "Domain-Driven Design" once, added at 54.99 EUR
- And the price of "Domain-Driven Design" has since been set to 49.99 EUR through the staff API
- When a client holding a staff token sends `GET /api/carts`
- Then Ada's cart in the answer has `total` 49.99
Test: DcaShop.IntegrationTests.CartApiTest#TheStaffListTotalsEachCartAtTodaysPrices
