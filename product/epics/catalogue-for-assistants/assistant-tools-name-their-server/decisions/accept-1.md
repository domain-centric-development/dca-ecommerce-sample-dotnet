---
id: assistant-tools-name-their-server-accept-1
story: assistant-tools-name-their-server
stage: document
kind: acceptance
asked: 2026-10-12T21:39:55Z
digest: b1bdfb69b11ab88d239e0baa53d08e69e32f06803c1ff4500e0350539808104b
---

# Accept assistant-tools-name-their-server?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- the-server-offers-two-catalogue-tools: Given the shop has started; When an assistant without any credentials connects to the MCP server at `/mcp` (streamable HTTP) and lists its tools; Then the server introduces itself as "dca-ecommerce-sample-mcp-server", version "1.0.0"; And it offers the tool `all-products` described as "Get all products in the catalog. Returns complete product information including SKU, name, price, category, and available stock."; And the tool `product-by-id` with one parameter `id`, described as "Get detailed product information by product ID. Requires the internal product UUID. Returns complete product details including all attributes." — `DcaShop.E2eTests.McpServerE2eTest#TheServerIntroducesItselfAndOffersTheTwoCatalogueTools`
- an-unknown-id-returns-no-product: Given the seeded sample catalogue; When an assistant calls `product-by-id` with `id` "no-such-product"; Then the call succeeds and its answer is `null` — `DcaShop.IntegrationTests.McpToolTest#AnIdThatNamesNoProductAnswersNull`
- a-blank-id-is-a-tool-error: Given the seeded sample catalogue; When an assistant calls `product-by-id` with `id` ""; Then the call ends in a tool error whose message is "Product ID cannot be null or blank" — `DcaShop.IntegrationTests.McpToolTest#ABlankIdIsAToolError`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-12T21:49:16Z
