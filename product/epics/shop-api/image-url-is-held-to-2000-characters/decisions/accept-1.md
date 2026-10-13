---
id: image-url-is-held-to-2000-characters-accept-1
story: image-url-is-held-to-2000-characters
stage: document
kind: acceptance
asked: 2026-10-13T16:39:37Z
digest: 91cd8e86acc237bd5501e13137bedbcaef2035f9c3b2a6b29c5b60c564531dd1
---

# Accept image-url-is-held-to-2000-characters?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- an-overlong-image-url-is-refused: Given a client holding a staff token; When it creates a product with SKU "IMG-001", name "Big Picture", an `imageUrl` of 2001 characters, price 9.99, category "Books" and stock 1; Then the shop answers 400 with an `application/problem+json` body; And its title is "Unacceptable value" and it carries no detail — `DcaShop.E2eTests.CreateProductApiE2eTest#AnImageUrlLongerThan2000CharactersIsRefusedAsAnUnacceptableValue`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Blömer
at: 2026-10-13T17:05:44Z
