---
id: an-unknown-product-in-the-body-is-unprocessable-accept-1
story: an-unknown-product-in-the-body-is-unprocessable
stage: document
kind: acceptance
asked: 2026-10-15T11:16:47Z
digest: c1be1abde8827b2a7e362849a9b2d91a62df5d9ddef96b4e18ffc75235ff859f
---

# Accept an-unknown-product-in-the-body-is-unprocessable?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- an-unknown-product-is-not-available: Given Ada's open, empty cart; When her client sends `POST /api/carts/<cartId>/items` with; Then the shop answers 422 with an `application/problem+json` body; And its title is "Article not available" and its detail; And her cart is still empty — `DcaShop.E2eTests.CartApiE2eTest#AProductTheCatalogueDoesNotKnowIsUnprocessableAndLeavesTheCartEmpty`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-15T14:56:00Z
rationale: Accepted by the person (2026-10-15), recorded by the managing session.
