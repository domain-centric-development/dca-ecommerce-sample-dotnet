---
id: created-product-answers-with-its-price-accept-1
story: created-product-answers-with-its-price
stage: document
kind: acceptance
asked: 2026-10-13T07:24:09Z
digest: 61efd4efe1d5fc75a48db8e87532f059e1004f15d56a1b3a434eef78a6fdbd6b
---

# Accept created-product-answers-with-its-price?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- staff-create-a-product: Given a client holding a token whose roles include staff; When it sends `POST /api/products` with `{"sku":"MUG-002","name":"Aggregate Mug","description":"A mug with a consistency boundary.","imageUrl":"","price":14.99,"category":"Desk & Office","stock":12}`; Then the shop answers 201; And the body has an `id`, `sku` "MUG-002", `name` "Aggregate Mug", `price` 14.99, `currency` "EUR" and `category` "Desk & Office" — `DcaShop.E2eTests.CreateProductApiE2eTest#AStaffMemberCreatesAProductAndTheAnswerCarriesItsPriceInEuros`
- a-product-page-without-description: Given staff created "Bounded Context Tote" without a description; When a visitor opens its product page; Then the description reads "No description available." — `DcaShop.IntegrationTests.ProductPageWithoutDescriptionTest#APageOfAProductWithoutDescriptionSaysThatThereIsNone`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-13T07:30:03Z
