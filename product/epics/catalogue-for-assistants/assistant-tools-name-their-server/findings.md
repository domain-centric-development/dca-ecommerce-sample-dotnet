---
story: assistant-tools-name-their-server
epic: catalogue-for-assistants
---

# Findings — assistant-tools-name-their-server

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | dca | src/DcaShop.Product/Adapter/Incoming/Mcp/ProductCatalogMcpToolProvider.cs:55 | minor | The found-product answer is now hand-serialized to imitate the SDK, and no test calls `product-by-id` for a known product | Cover it in `an-assistant-reads-the-catalogue` (adopted, depends on this story) or with an adapter test whose stub returns one product | done |
| 2 | plan | tests/DcaShop.IntegrationTests/McpToolTest.cs:173 | minor | The plan says a non-UUID is answered "without calling the port", but no test asserts that; a well-formed unknown UUID is also untested | Add `Assert.Empty(port.Queries)` here, plus a case with an unknown UUID that asserts one query | done |
| 3 | clean-code | tests/DcaShop.IntegrationTests/McpToolTest.cs:212 | minor | `AnswersNull` accepts two shapes (no content, or a "null" text block), but the adapter answers only the first | Assert the one shape: `Assert.Empty(result.Content)` | done |
| 4 | dca | tests/DcaShop.E2eTests/McpServerE2eTest.cs:105 | minor | `DisplayName` is not a title in the shared specification's scenarios.md; `SharedScenariosTest` fails when the build runs with `SpecificationPath` | The user adds the scenario to the specification and the Java suite uses the same title | done |
