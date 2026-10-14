---
id: reading-the-active-cart-opens-none-accept-1
story: reading-the-active-cart-opens-none
stage: document
kind: acceptance
asked: 2026-10-14T21:45:44Z
digest: c863bd4e88d07e60b834d833393f0d284fae28055302a2ee678cb9913a6133f1
---

# Accept reading-the-active-cart-opens-none?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- no-active-cart-is-not-found: Given a client holding the token of the customer "grace@example.com", who has no cart yet, and her `customerId`; When it sends `GET /api/carts/customer/<her customerId>/active` with her token; Then the shop answers 404 — `DcaShop.E2eTests.CartApiE2eTest#ReadingTheActiveCartOfACustomerWithoutOneIsNotFound`
- a-read-leaves-no-cart-behind: Given the client of "grace@example.com" has just been answered 404 for her active cart; When it sends `POST /api/carts` with her token; Then the shop answers 201; And the body has `items` [], `status` "ACTIVE" and `itemCount` 0 — `DcaShop.IntegrationTests.CartApiTest#ReadingTheActiveCartLeavesNoCartBehindSoOpeningOneIsCreated`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-14T21:48:28Z
rationale: Accepted on the person's behalf by the managing session (authorized 2026-10-14 21:35 for the remaining WP-108 queue).
