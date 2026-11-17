---
story: assistant-tools-name-their-server
epic: catalogue-for-assistants
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (4 rows, 0 without a fitting value)
---

# Findings — assistant-tools-name-their-server

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | dca | src/DcaShop.Product/Adapter/Incoming/Mcp/ProductCatalogMcpToolProvider.cs:55 | minor | introduced |  |  | The found-product answer is now hand-serialized to imitate the SDK, and no test calls `product-by-id` for a known product | Cover it in `an-assistant-reads-the-catalogue` (adopted, depends on this story) or with an adapter test whose stub returns one product | done | test | checking | missing |
| 2 | plan | tests/DcaShop.IntegrationTests/McpToolTest.cs:173 | minor | introduced |  |  | The plan says a non-UUID is answered "without calling the port", but no test asserts that; a well-formed unknown UUID is also untested | Add `Assert.Empty(port.Queries)` here, plus a case with an unknown UUID that asserts one query | done | test | checking | missing |
| 3 | clean-code | tests/DcaShop.IntegrationTests/McpToolTest.cs:212 | minor | introduced |  |  | `AnswersNull` accepts two shapes (no content, or a "null" text block), but the adapter answers only the first | Assert the one shape: `Assert.Empty(result.Content)` | done | test | checking | incorrect |
| 4 | dca | tests/DcaShop.E2eTests/McpServerE2eTest.cs:105 | minor | introduced |  |  | `DisplayName` is not a title in the shared specification's scenarios.md; `SharedScenariosTest` fails when the build runs with `SpecificationPath` | The user adds the scenario to the specification and the Java suite uses the same title | done | story | traceability | missing |
