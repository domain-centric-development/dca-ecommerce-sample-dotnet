---
id: price-rounding-to-zero-is-unacceptable-accept-1
story: price-rounding-to-zero-is-unacceptable
stage: document
kind: acceptance
asked: 2026-10-13T16:52:08Z
digest: 9f2ccc847e4491c011e54f6f98b30ac8ea4704a0986f8cf7d1f257b3564188bf
---

# Accept price-rounding-to-zero-is-unacceptable?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- a-price-that-rounds-to-zero-is-refused: Given a client holding a staff token; When it creates "Tiny Mug" with SKU "TINY-001", price 0.001, category "Desk & Office" and stock 5; Then the shop answers 400 with an `application/problem+json` body; And its title is "Unacceptable value" and it carries no detail — `DcaShop.E2eTests.CreateProductApiE2eTest#APriceBelowACentIsRefusedAsAnUnacceptableValue`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Blömer
at: 2026-10-13T17:06:55Z
