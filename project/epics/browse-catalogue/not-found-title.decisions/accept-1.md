---
id: not-found-title-accept-1
story: not-found-title
stage: document
kind: acceptance
asked: 2026-09-26T11:03:44Z
digest: 60840f28b8ab2b14e7afbca6dfe95c2d1aa673c573d2a297865b147f966b8026
---

# Accept not-found-title?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- not-found-tab-reads-the-shop-name: Given the seeded sample catalogue, in which no product has the id "no-such-product"; When a visitor opens the product page `/products/no-such-product`; Then the browser tab title is "domaincentric.commerce" — `DcaShop.E2eTests.NotFoundTitleE2eTest#NotFoundPageIsTitledWithTheShopName`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-09-26T11:05:47Z
