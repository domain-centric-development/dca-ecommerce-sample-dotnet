---
id: a-malformed-product-id-is-a-bad-request-accept-1
story: a-malformed-product-id-is-a-bad-request
stage: document
kind: acceptance
asked: 2026-10-14T17:13:16Z
digest: 5764614f28cdcc20b3a52c248affac5eb4307f55851cec13c817707ddb829b3b
---

# Accept a-malformed-product-id-is-a-bad-request?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- a-malformed-product-id-is-a-bad-request: Given Ada's open, empty cart; When her client sends `POST /api/carts/<cartId>/items` with `{"productId":"no-such-product","quantity":1}`; Then the shop answers 400 with an `application/problem+json` body; And its title is "Unacceptable value" and it carries no detail; And her cart is still empty — `DcaShop.E2eTests.CartApiE2eTest#AProductIdThatIsNotAUuidIsABadRequestAndLeavesTheCartEmpty`
- removing-a-malformed-product-id-is-a-bad-request: Given Ada's cart holds "Domain-Driven Design" × 1; When her client sends `DELETE /api/carts/<cartId>/items/no-such-product`; Then the shop answers 400 with an `application/problem+json` body; And its title is "Unacceptable value" and it carries no detail; And her cart still holds "Domain-Driven Design" × 1 — `DcaShop.IntegrationTests.CartItemRemovalResourceTest#RemovingAProductIdThatIsNotAUuidIsABadRequestAndLeavesTheLine`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-14T17:14:43Z
