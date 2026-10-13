---
story: forms-refuse-a-post-without-token-with-403
epic: fits-its-surroundings
---

# Findings — forms-refuse-a-post-without-token-with-403

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | dca | docs/architecture/adr/adr-005-antiforgery-and-safe-methods.md:17 | minor | The amended decision says `AutoValidateAntiforgeryTokenAttribute` rejects with `403`; that filter was replaced (adr-007:53) and answers `400` — `TokenOnlyAwareAntiforgeryFilter` gives the `403`. | Name `TokenOnlyAwareAntiforgeryFilter` (ADR-007) as the filter that answers `403` in the amendment. | open |
| 2 | clean-code | tests/DcaShop.IntegrationTests/FormTokenRefusalTest.cs:197 | minor | `Submission`/`Fields`/`Text` are one more private copy of the token-scraping form helpers kept in many integration test classes (e.g. `AccountFlowTest.cs:284`). | Extract one shared test-support helper (the asserting `Submission` variant) and use it here; migrate the older copies later. | open |
