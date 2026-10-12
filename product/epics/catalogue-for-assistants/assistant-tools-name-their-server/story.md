---
id: assistant-tools-name-their-server
epic: catalogue-for-assistants
status: approved
context: product
feature: catalogue
title: The assistant tools name their server and take any id
depends_on: [CAT-01]
---

# The assistant tools name their server and take any id

## Story

As an AI assistant I want the shop's tool server to say who it is and to answer any product id I pass, so that I
can tell the shop's server apart and ask about a product without knowing how its ids are built.

## Acceptance criteria

### Rule: The shop offers its catalogue as two tools, to anyone

#### the-server-offers-two-catalogue-tools (happy path)
- Given the shop has started
- When an assistant without any credentials connects to the MCP server at `/mcp` (streamable HTTP) and lists its tools
- Then the server introduces itself as "dca-ecommerce-sample-mcp-server", version "1.0.0"
- And it offers the tool `all-products` described as "Get all products in the catalog. Returns complete product information including SKU, name, price, category, and available stock."
- And the tool `product-by-id` with one parameter `id`, described as "Get detailed product information by product ID. Requires the internal product UUID. Returns complete product details including all attributes."

### Rule: product-by-id answers any id

#### an-unknown-id-returns-no-product
- Given the seeded sample catalogue
- When an assistant calls `product-by-id` with `id` "no-such-product"
- Then the call succeeds and its answer is `null`

#### a-blank-id-is-a-tool-error
- Given the seeded sample catalogue
- When an assistant calls `product-by-id` with `id` ""
- Then the call ends in a tool error whose message is "Product ID cannot be null or blank"

## Changed expectations

- The server introduces itself with the SDK's default name now and as "dca-ecommerce-sample-mcp-server", version "1.0.0" after.
- `product-by-id` takes only a UUID now — "no-such-product" and "" fail at argument binding — and takes any string after: an unknown id answers `null`, a blank one is a tool error.

## Out of scope

- The two tools' answers for a known product and for the whole range — `an-assistant-reads-the-catalogue`, which adopts them.

## Notes

- The shop in the other language already behaves like this; this story closes the gap so that both hold the same spec
  (WP-107, decision `an-assistant-reads-the-catalogue-01`).
