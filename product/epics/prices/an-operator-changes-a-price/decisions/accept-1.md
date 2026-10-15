---
id: an-operator-changes-a-price-accept-1
story: an-operator-changes-a-price
stage: document
kind: acceptance
asked: 2026-10-15T09:39:34Z
digest: 87d1b2df25c272a86e3269b7d259f953ab61636fea5ae16f3c48e976718815ce
---

# Accept an-operator-changes-a-price?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- a-changed-price-shows-on-the-product-page: Given a client holding a staff token, and "Domain-Driven Design" costs 54.99 EUR; When it sends `PUT /api/products/<id of Domain-Driven Design>/price` with `{"price":49.99}`; Then the product page of "Domain-Driven Design" shows "49.99 EUR"; And its catalogue card shows "49.99 EUR" — `DcaShop.E2eTests.ChangePriceE2eTest#AChangedPriceIsShownOnTheProductPageAndTheCard`
- a-stranger-cannot-change-a-price: Given a client without a token, and "Domain-Driven Design" costs 54.99 EUR; When it sends `PUT /api/products/<id of Domain-Driven Design>/price` with `{"price":49.99}`; Then the shop answers 401 with the header `WWW-Authenticate: Bearer`; And the product page of "Domain-Driven Design" still shows "54.99 EUR" — `DcaShop.IntegrationTests.PriceChangeTest#AStrangerIsChallengedAndThePriceStays`
- a-customer-cannot-change-a-price: Given a client holding the customer token of Ada, and "Domain-Driven Design" costs 54.99 EUR; When it sends `PUT /api/products/<id of Domain-Driven Design>/price` with `{"price":49.99}`; Then the shop answers 403; And the product page of "Domain-Driven Design" still shows "54.99 EUR" — `DcaShop.IntegrationTests.PriceChangeTest#ACustomerIsForbiddenAndThePriceStays`
- a-new-price-of-zero-is-refused: Given a client holding a staff token, and "Domain-Driven Design" costs 54.99 EUR; When it sends `PUT /api/products/<id of Domain-Driven Design>/price` with `{"price":0}`; Then the shop answers 400 with an `application/problem+json` body; And its title is "Unacceptable value" and its `errors` names `price` with the reason; And the product page of "Domain-Driven Design" still shows "54.99 EUR" — `DcaShop.IntegrationTests.PriceChangeTest#APriceOfZeroIsAnUnacceptableValueNamingThePriceAndThePriceStays`
- a-new-price-of-one-cent-is-accepted: Given a client holding a staff token; When it sends `PUT /api/products/<id of Domain-Driven Design>/price` with `{"price":0.01}`; Then the product page of "Domain-Driven Design" shows "0.01 EUR" — `DcaShop.IntegrationTests.PriceChangeTest#APriceOfOneCentIsTakenAndShownOnTheProductPage`
- changing-the-price-of-an-unknown-product-finds-nothing: Given a client holding a staff token, and no product with the id "00000000-0000-4000-8000-000000000001"; When it sends `PUT /api/products/00000000-0000-4000-8000-000000000001/price` with `{"price":49.99}`; Then the shop answers 404 — `DcaShop.IntegrationTests.ProductPriceResourceTest#AProductWithoutAPriceIsNotFound`
- a-malformed-product-id-in-the-price-address-is-a-bad-request: Given a client holding a staff token; When it sends `PUT /api/products/no-such-product/price` with `{"price":49.99}`; Then the shop answers 400 with an `application/problem+json` body; And its title is "Unacceptable value" — `DcaShop.IntegrationTests.ProductPriceResourceTest#AProductIdThatIsNotAUuidIsAnUnacceptableValue`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-15T14:56:00Z
rationale: Accepted by the person (2026-10-15), recorded by the managing session.
