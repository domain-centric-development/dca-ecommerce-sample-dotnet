---
id: catalogue-title-01
story: catalogue-title
stage: build
asked: 2026-09-26T06:20:20Z
---

# The catalogue heading's `data-test`: the judge's fix breaks two existing integration tests

## Question

The judge (round 1, `tasks/catalogue-title/judge.md`, Confirmed defects) says
`tests/DcaShop.E2eTests/Pages/ProductCatalogPage.cs:61-62` finds the catalogue's `<h1>` by ARIA role. That breaks
`project/product.md`, "Look and feel": "every element a test addresses carries a stable `data-test` attribute". The
judge's fix is `<h1 data-test="catalog-heading">Our Products</h1>` in `src/DcaShop.Web/Views/Product/Catalog.cshtml:8`.

Two existing integration tests match the heading's literal markup, and the fix would break both:

- `tests/DcaShop.IntegrationTests/ProductPageTest.cs:70` —
  `Assert.Contains("<h1>Our Products</h1>", …, StringComparison.Ordinal)`
- `tests/DcaShop.IntegrationTests/ProductPageTest.cs:85` — `Text(followed, @"<h1>([^<]*)</h1>")`

The plan (`tasks/catalogue-title/plan.md`, Files) says these tests "stay green and must not change". The build stage
may not change a test to make it pass. So this stage cannot make the fix alone. How should the defect be resolved?

## Options

1. **Add the attribute and loosen the two integration tests.** The tests would match the heading's text, not the bare
   `<h1>` markup (e.g. `<h1\b[^>]*>([^<]*)</h1>`). This is what the judge proposed. It changes tests that belong to
   another story (`ProductPageTest.cs` is uncommitted, from the CAT stories), so it overrides the plan's "must not
   change". The Java template needs the same attribute (the views' one-to-one sync duty).
2. **Drop the heading assertion from the browser test.** `ProductPageTest.cs:70/85` already asserts "Our Products".
   No markup changes and no Java sync is needed. It departs from the plan's "asserted in the same happy-path test".
   It is a test-stage change.
3. **Wrap the heading:** `<div data-test="catalog-heading"><h1>Our Products</h1></div>`. Both integration tests stay
   green, and the page object reads the wrapper through `data-test`. This adds markup whose only purpose is testing,
   diverges from the Java template's structure unless it is mirrored there, and could affect layout selectors.
4. **Accept the role locator** as an exception to "Look and feel" for this one heading, and record why.

## Recommendation

Option 2. The story is about the document title. The heading is only an assumption check (CAT-01-01) that the
integration suite already covers. Option 2 changes no production markup, keeps the Java views in sync, and leaves the
other story's tests alone. If the heading should stay in the browser test, choose option 1 and record that it
overrides the plan's line on `ProductPageTest.cs`.

## Answer
answer: 2 — drop the heading assertion from the browser test; the integration tests already assert "Our Products". No markup changes, the views stay as the Java templates are.
applies: test
by: Christoph Bloemer
at: 2026-09-26T06:40:42Z
rationale: The story is about the document title; the heading is covered where it already is.

## Applied
at: 2026-09-26T06:48:12Z
stage: build
