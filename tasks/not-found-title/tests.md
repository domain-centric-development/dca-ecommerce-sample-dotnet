# Tests — not-found-title

Carried in-session using the `e2e-testing` craft (`carrier.test`). The skill was not invoked separately here.
`dca-knowledge` was not asked: the story changes a view's document title and raises no pattern question.

<!-- gate:tests -->
| criterion | test |
| --- | --- |
| not-found-tab-reads-the-shop-name | DcaShop.E2eTests.NotFoundTitleE2eTest#NotFoundPageIsTitledWithTheShopName |

## Files
- tests/DcaShop.E2eTests/NotFoundTitleE2eTest.cs
- tests/DcaShop.E2eTests/Pages/NotFoundPage.cs
- src/DcaShop.Web/Views/Error/404.cshtml — only restored to its committed version: an earlier build run had left an uncommitted edit there (see Notes). This stage made no production change.

## Notes
- NotFoundTitleE2eTest#NotFoundPageIsTitledWithTheShopName: follows the scenario. It opens
  `/products/no-such-product` and asserts the tab title "domaincentric.commerce". The revised plan asks this
  test to also check the details CAT-03-01 keeps: the code "404", the heading "Page Not Found", the message
  "The page you're looking for doesn't exist or has been removed. Perhaps you were looking for one of our
  products?", "Browse All Products" → `/products` and "Go to Homepage" → `/`. These assertions were added in
  this run.
- NotFoundTitleE2eTest#NotFoundPageIsTitledWithTheShopName: currently fails on
  `Assert.Equal("domaincentric.commerce", …)`, which gets "Page Not Found". The reason is that
  `404.cshtml` still sets `ViewData["Title"] = "Page Not Found"`. The gate had refused the stage (tests-red)
  because an earlier build run of this story had left its one-line view change uncommitted in the working tree.
  This run put that line back, so the view is the committed version again (`git diff` shows no change to the
  file). The build stage makes that change again. The detail assertions hold on both versions of the view.
  They are not what turns the test red.
- `NotFoundPage` waits for the path the visitor opened, because the status-code re-execute keeps the URL. It also
  waits for `error-browse-link`, because the error page's container has no `data-test`. The code, heading and
  message have no `data-test` either. They are read as visible body text with whitespace collapsed, since the
  message breaks across lines in the markup. Adding `data-test` attributes would change production markup,
  which is not this stage's job.
- unit tests: none. The plan names no invariant, and no domain type changes.
- No integration test: the plan lists no adapter as changed. `ShopFlowTest.UnknownPageRendersThe404Page` already
  covers the 404 status and body, and it stays untouched.
- `BasePage.cs` was not touched. Its uncommitted changes belong to other stories.
- `formatFix` (`dotnet format DcaShop.sln`) ran. It changed only the using-block spacing in `NotFoundPage.cs`.
- Open, as the plan says: the specification's `scenarios.md` must carry this scenario title, or `SharedScenariosTest`
  fails when it runs with `-p:SpecificationPath=…`. That file is outside this checkout, so it was not checked.
