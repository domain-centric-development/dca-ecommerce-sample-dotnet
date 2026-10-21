---
id: the-cart-holds-no-more-than-is-in-stock-accept-1
story: the-cart-holds-no-more-than-is-in-stock
stage: document
kind: acceptance
asked: 2026-10-21T06:14:20Z
digest: 8457e1c1b6a8dcbb4e429efbb4da2f6621d9a6c16c8848f8a76ab34f8ff3e309
---

# Accept the-cart-holds-no-more-than-is-in-stock?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- adding-past-the-stock-on-the-page-is-refused: Given the seeded "Wooden Hexagon Desk Model" with a stock of 8; And a guest whose cart holds "Wooden Hexagon Desk Model" with quantity 8; When the guest presses "Add to Cart" on its product page; Then the cart page opens with the error "There is not enough stock to add 1 of this product — your cart already holds 8, and 8 are in stock."; And the line "Wooden Hexagon Desk Model" still shows quantity 8 — `DcaShop.E2eTests.AddPastTheStockE2eTest#AddingOneUnitMoreThanTheStockIsRefusedAndTheLineKeepsItsQuantity`
- adding-past-the-stock-through-the-api-is-refused: Given Ada's open cart holds "Wooden Hexagon Desk Model" with quantity 5, and 8 are in stock; When her client adds "Wooden Hexagon Desk Model" with quantity 4; Then the shop answers 409 with an `application/problem+json` body; And its title is "Not enough stock" and its detail "Requested 4 of product <id of Wooden Hexagon Desk Model>, more than is in stock."; And her cart still holds "Wooden Hexagon Desk Model" with quantity 5 — `DcaShop.IntegrationTests.CartApiTest#AddingPastTheStockWithWhatTheCartHoldsIsAConflictAndLeavesTheLine`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-21T06:29:23Z
