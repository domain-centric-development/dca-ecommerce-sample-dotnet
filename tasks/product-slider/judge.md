# Judge — product-slider

Round: acceptance correction product-slider-accept-1 (8 cards; 4 in view on xl and l, 2 on m, 1 on s; paging by one
card; Previous disabled at the start, Next at the end; no auto-play). Reviewed: the working-tree diff against HEAD
(`ProductSelection.MaxSize` 4 → 8, one `@media (max-width: 768px)` rule in `main.css`, the changed and new tests) and
the story's whole footprint from the first delivery (`50730b9`), which the previous verdict judged.

## Verdict
verdict: pass

## Perspectives covered
- ddd: `review-ddd` (skill, named in the profile as `review.ddd`)
- hexagonal: `review-hexagonal` (skill, named in the profile as `review.hexagonal`)
- clean-code: `review-clean-code` (skill, named in the profile as `review.clean-code`)
- dca: `dca-review` (skill, named in the profile as `review.dca`, added via `reviews: dca`)

Knowledge source `dca-knowledge` (profile) was not needed: this round takes no architecture decision. The shape of
the slider (read use case, fragment adapter, value object) is unchanged and rests on the nodes the plan cites
(`/decision/read-model-vs-domain-query.md`, `/recipe/add-a-read-model.md`, `/rule/hexagonal/dca-hex-011.md`,
`/rule/hexagonal/dca-hex-007.md`).

## Confirmed defects
| Perspective | File:line | Severity | Defect | Fix |
| --- | --- | --- | --- | --- |
| clean-code | tests/DcaShop.E2eTests/Pages/HomePage.cs:203-235 | minor | `SettlesEnabledAsync` repeats `SettlesDisabledAsync` line for line. Only the expected `disabled` value differs. | Optional: one `SettlesAsync(string dataTest, bool disabled)` behind the four public `Is…EnabledAsync` / `Is…DisabledAsync` methods. Not blocking. |

No blocker and no major defect.

## Considered and dropped
- ddd, `src/DcaShop.Product/Domain/glossary.md:108-112` still says "Up to four different products" while
  `ProductSelection.MaxSize` is 8. This is a real drift, but the plan (`## Glossary proposals`), build.md and tidy.md
  assign it to the document stage, which runs after this one. It is not a code defect of this round. The document
  stage must change it.
- ddd, `ProductSelection` (`ProductSelection.cs:10`): only the constant changes. The doc comment references
  `<see cref="MaxSize"/>` rather than a number, so no comment went stale. The invariant (up to `MaxSize` distinct
  priced candidates) still sits in the value object.
- hexagonal: no layer, port, adapter or dependency changed. The view component and the use case take the count from
  the draw (`GetProductSelectionUseCase`), so nothing outside the domain restates "8".
- clean-code, `main.css` 768 px rule: it has the same `calc` shape and the same `--product-slider-gap` variable as the
  default rule. It sits before the 480 px rule, so size s still wins. The breakpoints are the product description's
  sizes (`project/product.md:38-43`: s ≤ 480, m ≤ 768).
- clean-code, the new tablet test classes duplicate the side-by-side assertions of the desktop test (three copies of
  the top and left position checks). Each class carries one viewport, the pattern `HomeSliderPhoneE2eTest` already
  set. The shape is test assertions, not a decision carried twice. Dropped.
- dca, twin checkpoint: `main.css` now diverges from the Java sample's copy by the size-m rule. This is covered by
  the approved exception `scenario.home.slider-*` (`../dca-sample-specification/exceptions.md:7`) and is named in
  `README.md:156-158`. The story's `## Out of scope` says the Java sample follows later. This is not a silent fork.
- Changed tests against `## Changed tests`:
  - `HomeSliderE2eTest`: the eight-card test, `DrawsItsProductsAnewOnEveryRequest` and the desktop test follow from
    "up to 8" and "4 of its 8 cards, Next enabled". The desktop test's added `Assert.Equal(8, CardCountAsync())`
    follows from "4 of its 8 cards".
  - `HomeSliderPhoneE2eTest#OnAPhoneTheSliderStopsAtItsLastCard`: seven presses, eighth card.
  - `HomePage`: the settled "enabled" check.
  - `ProductSelectionTest` and `GetProductSelectionUseCaseTest`: 8 instead of 4.
  - `ProductSliderTest`: 10 priced, 8 cards expected.
  - Each change follows from its backing line. The `IsNextEnabledAsync` precondition before each press in the phone
    test is a guard that "Next" can be pressed, implied by pressing it seven times. It expects nothing beyond the
    backing line.
- Browser-runner rule: every new and changed end-user test drives Playwright and judges from rendered boxes and button
  state (`HomePage.cs:25-37`, `:151-172`), never from markup text.
- Product and technical description: no new surface, persistence or client framework. The inline paging script is
  unchanged. Nothing from `## Out of scope` is delivered (no add-to-cart, no popularity, no personalisation, no
  REST/MCP).
- Scenario titles: all sixteen `DisplayName`s equal the titles in `../dca-sample-specification/scenarios.md:186-303`,
  including the four new or retitled ones (`slider-eight-different-products`, `slider-desktop`,
  `slider-tablet-four-cards`, `slider-small-tablet-two-cards`, `slider-desktop-next`,
  `slider-desktop-stops-at-the-end`).

## Criteria re-checked
- shows-discover-products-slider-below-hero: met. The test is unchanged (`HomeSliderE2eTest.cs:15-24`).
- slider-holds-eight-different-products: met. It asserts 8 cards, 8 distinct names, and every name in the catalogue
  (`HomeSliderE2eTest.cs:26-37`).
- products-are-drawn-anew-per-request: met. It notes 8 products and asserts at least one of 10 reloads differs
  (`HomeSliderE2eTest.cs:39-59`). 8 of 21 are drawn.
- product-without-price-is-not-offered: met. With 10 of 21 priced, every one of 10 requests shows 8 cards, all priced.
  The draw is a strict choice (`ProductSliderTest.cs:29-45`).
- shows-the-priced-products-there-are: met. The test is unchanged: 2 priced products give exactly those 2 cards.
- card-shows-image-name-and-price: met. The test is unchanged.
- card-links-to-product-page: met. The test is unchanged.
- desktop-shows-four-cards-side-by-side: met. It asserts exactly `[0,1,2,3]` in view (so cards 4-7 are not), the
  same top and increasing left, "Previous" disabled and "Next" enabled (`HomeSliderE2eTest.cs:93-106`).
- size-l-shows-four-cards-side-by-side: met. At 1024 × 768 it asserts exactly `[0,1,2,3]` in view and the cards side
  by side (`HomeSliderTabletE2eTest.cs:19-30`).
- size-m-shows-two-cards-side-by-side: met. At 768 × 1024 it asserts exactly `[0,1]` in view and the cards side by
  side (`HomeSliderSmallTabletE2eTest.cs:19-30`).
- phone-shows-one-card-at-a-time: met. The test is unchanged: exactly `[0]` in view, "Previous" disabled.
- desktop-next-moves-by-one-card: met. From `[0..3]`, one press gives exactly `[1..4]` (the first card is out) and
  "Previous" is enabled (`HomeSliderE2eTest.cs:108-121`).
- desktop-next-is-disabled-at-the-last-card: met. After each of four presses the window shifts by one. It ends at
  exactly `[4..7]` with "Next" disabled (`HomeSliderE2eTest.cs:123-139`).
- next-brings-the-following-card-into-view: met. The test is unchanged.
- previous-brings-the-preceding-card-into-view: met. The test is unchanged.
- next-is-operable-by-keyboard: met. The test is unchanged.
- next-is-disabled-at-the-last-card: met. Seven presses give exactly `[7]` with "Next" disabled
  (`HomeSliderPhoneE2eTest.cs:63-77`).
- slider-does-not-move-by-itself: met. The test is unchanged.
- Guard (story Assumptions line 2, no priced product → no slider): `ProductSliderTest#WithoutAnyPricedProduct…` is
  unchanged.

## Previous round
- The previous verdict (`.judge-previous.md`) was `pass` with no confirmed defects, so there is nothing to account
  for. Its dropped items still hold for the unchanged footprint:
  - `System.Random` passed into `ProductSelection.Draw`
  - the value-equality override
  - `EnrichedProduct` in the result
  - the view component composed by name
  - the double pricing call accepted in the plan
  - the documented `TimeoutException` catch in `HomePage.CardsInViewAsync`
