# Judge — CAT-03

Adopt mode (`status: adopted`): the change is the claim that each mapped test proves its scenario. Every test was
read against its scenario; nothing was built, so nothing else was reviewed.

## Verdict
verdict: pass

## Perspectives covered
- ddd: not run — adopt mode reviews only the claim that each test proves its scenario (carrier `review-ddd`
  available, not needed)
- hexagonal: not run — adopt mode (carrier `review-hexagonal` available, not needed)
- clean-code: not run — adopt mode (carrier `review-clean-code` available, not needed)
- dca (added): not run — adopt mode (carrier `dca-review` available, not needed)
- scenario-against-test reading: in-session
- knowledge (`dca-knowledge`, named in the profile): not consulted — adoption raises no pattern question

## Confirmed defects
| Perspective | File:line | Severity | Defect | Fix |
| --- | --- | --- | --- | --- |
| test | tests/DcaShop.E2eTests/NotFoundTitleE2eTest.cs:21 | minor | "the heading 'Page Not Found'" is asserted as text anywhere in `body` (`NotFoundPage.ShowsAsync`, `Pages/NotFoundPage.cs:30-31`), not as a heading. It still proves the scenario today, because the title no longer carries the words and `404.cshtml:3` is the only place in the body that does. | Add a page-object method that reads the `h2.error-page__title` text (or a `data-test` on it) and assert against that. |

## Considered and dropped
- `not-found-page-has-the-shop-title` mapped to a browser test although the plan put it at integration level: the
  test is the one happy-path browser test that `not-found-title` already brought in; no extra browser test was added
  for a non-happy-path scenario, so the test-level rule is not broken.
- The two integration tests read markup with regexes (`NotFoundPageTest.cs:59-72`) although the profile names a
  browser runner: they are integration tests, not end-user tests, and the non-happy-path scenarios belong at that
  level. They follow the link's `href` for real and assert the target's `<h1>`.
- The `Link` regex (`NotFoundPageTest.cs:60`) depends on `href` coming before `data-test`: fragile, but it fails
  loudly (`Assert.True(match.Success, …)`) rather than passing silently. Not a proof defect.
- "404" asserted as body text (`NotFoundTitleE2eTest.cs:20`): the scenario says "the page shows '404'", which is
  exactly what is asserted.
- The story diff also carries `not-found-title` (view, E2E test, page object): that story's own judge covered it;
  CAT-03 depends on it (`depends_on`), and the e2e test is green with the expanded assertions
  (`tasks/not-found-title/.verify/gate-build.111337.txt`, and `tasks/CAT-03/.verify/gate-test.112044.txt`).
- Nothing listed under the story's `## Out of scope` (API, MCP tools) is touched.

## Criteria re-checked
- unknown-product-shows-the-not-found-page: met — `NotFoundTitleE2eTest.cs:17` opens `/products/no-such-product`
  against the seeded shop; lines 20-23 assert "404", "Page Not Found" and the full message (whitespace collapsed,
  matching `404.cshtml:4-5`). Heading asserted as text only (minor, above).
- not-found-page-has-the-shop-title: met — `NotFoundTitleE2eTest.cs:19` asserts the document title
  "domaincentric.commerce".
- browse-all-products-leads-to-the-catalogue: met — `NotFoundPageTest.cs:24-35` opens the not-found page (asserts
  404), finds the link by `data-test`, asserts the label "Browse All Products", follows its `href` and asserts the
  `<h1>` "Our Products"; the break patch turns it red.
- go-to-homepage-leads-to-the-home-page: met — `NotFoundPageTest.cs:37-48` does the same for "Go to Homepage" and
  asserts the rendered `<h1>` "Welcome to domaincentric.commerce"; the break patch turns it red.
