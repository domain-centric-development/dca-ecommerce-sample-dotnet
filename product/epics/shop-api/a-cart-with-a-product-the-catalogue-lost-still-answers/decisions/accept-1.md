---
id: a-cart-with-a-product-the-catalogue-lost-still-answers-accept-1
story: a-cart-with-a-product-the-catalogue-lost-still-answers
stage: document
kind: acceptance
asked: 2026-11-11T02:12:52Z
digest: 322f325e5ec31fd626d8f095e52958b4960992d4cf02864e2b207b6c428691f6
---

# Accept a-cart-with-a-product-the-catalogue-lost-still-answers?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- a-cart-answers-with-a-product-the-catalogue-lost: Given a cart holding "Clean Architecture" once and "Domain-Driven Design" once; And the catalogue no longer knows "Domain-Driven Design"; When "Clean Architecture" is added once more through the API; Then the answer is 200 and the cart holds "Clean Architecture" with quantity 2; And the line of "Domain-Driven Design" holds `quantity` 1, `available` false and no `price`; And `total` is the price of the two units of "Clean Architecture" alone — `DcaShop.E2eTests.CartApiE2eTest#AddingToACartWhoseOtherProductTheCatalogueLostAnswersTheCartWithThatLineUnpriced`
- the-staff-list-answers-with-a-product-the-catalogue-lost: Given the cart above; When a member of staff reads the list of every cart; Then the answer is 200 and lists the cart, its line of "Domain-Driven Design" with `available` false and no `price` — `DcaShop.IntegrationTests.CartOversightLostProductTest#TheStaffListKeepsALineWhoseProductTheCatalogueLostUnavailableAndWithoutAPrice, DcaShop.IntegrationTests.CartOversightResourceTest#TheStaffListAnswersALineWhoseProductTheCatalogueLostAsUnavailableWithoutAPrice`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.
