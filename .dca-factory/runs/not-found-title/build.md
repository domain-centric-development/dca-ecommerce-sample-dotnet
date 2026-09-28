# Build — not-found-title

Done in this session. The profile names `carrier.build: dca-modelling`, but it was not used: the change touches no
domain type, so there was no modelling decision to make. The guard `carrier.guard: dca-discipline` was applied to
the one file written. That file is a Razor view in the web host and sits outside every DCA layer. It has no domain,
application or adapter code, no imports and no cross-context reference, so no invariant applies to it and none is
broken. `dca-knowledge` (the profile's `knowledge:`) was not asked, because no part of this change depends on a
pattern question.

## Changed
| File | Why |
| --- | --- |
| src/DcaShop.Web/Views/Error/404.cshtml | Removed line 1 (`ViewData["Title"] = "Page Not Found"`). The layout (`Views/Shared/_Layout.cshtml:9`) now falls back to "domaincentric.commerce". The body stays as it was, as CAT-03-01 requires: the "404" code, the heading "Page Not Found", the message and both links. |

## Criteria
- not-found-tab-reads-the-shop-name: met. `/products/no-such-product` matches no route, and status-code pages
  re-execute it as `/error/404`. That view no longer sets a title, so the layout renders
  `<title>domaincentric.commerce</title>`. The body of the page is unchanged.

## Checks
- dotnet build: 0 errors
- dotnet test tests/DcaShop.E2eTests --filter "FullyQualifiedName~NotFoundTitleE2eTest.NotFoundPageIsTitledWithTheShopName": 1 passed
- dotnet test tests/DcaShop.UnitTests: 178 passed, 4 skipped
- dotnet test tests/DcaShop.IntegrationTests: 83 passed, 1 skipped
- dotnet test tests/DcaShop.ArchitectureTests: 129 passed
- dotnet test tests/DcaShop.E2eTests: 43 passed, 1 skipped
- dotnet format (formatFix), then dotnet format --verify-no-changes: clean, no file reformatted
