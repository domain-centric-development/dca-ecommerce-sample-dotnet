# Judge — product-slider

## Verdict
verdict: pass

## Perspectives covered
- ddd: `review-ddd` (skill, named in the profile as `review.ddd`)
- hexagonal: `review-hexagonal` (skill, named in the profile as `review.hexagonal`)
- clean-code: `review-clean-code` (skill, named in the profile as `review.clean-code`)
- dca: `dca-review` (skill, named in the profile as `review.dca`, added via `reviews: dca`)

Knowledge source `dca-knowledge` (profile) was not needed: no finding depended on a pattern, pitfall or recipe
beyond the plan's cited nodes (`/decision/read-model-vs-domain-query.md`, `/recipe/add-a-read-model.md`,
`/rule/hexagonal/dca-hex-011.md`, `/rule/hexagonal/dca-hex-007.md`), which the code follows.

## Confirmed defects
| Perspective | File:line | Severity | Defect | Fix |
| --- | --- | --- | --- | --- |

None.

## Considered and dropped
- ddd, `ProductSelection` in `Domain/Model` taking `System.Random`: a pure BCL strategy passed in as a parameter,
  not a field and not a gateway. The value holds the story rule "up to four different priced products"
  (`ProductSelection.cs:10,24-38`). Not a defect.
- ddd, `ProductSelection.Equals`/`GetHashCode` override (`ProductSelection.cs:40-42`) with no caller: this is
  value-object equality over the id list. Without it the record would compare the list by reference. It belongs
  to the value's shape and is not speculative generality.
- ddd, glossary has no "Product selection" / "Product slider" entry yet: the plan proposes both under
  `## Glossary proposals`, and entering them is the document stage's job.
- hexagonal, `GetProductSelectionResult(IReadOnlyList<EnrichedProduct>)`: `EnrichedProduct` is a `sealed record : IValue`
  read model. It is not an aggregate root and has the same shape as `GetAllProductsResult`, and DCA-USE-015 passes
  in the architecture suite. Not a defect.
- hexagonal, `ProductSliderViewComponent` (ASP.NET `ViewComponent`) in `Adapter/Incoming/Web`: it depends only on
  `IGetProductSelectionInputPort` and maps the result to a view model of primitives at the edge. Portal gets no C#
  reference (the view invokes the component by name, like `MiniBasket`). This matches `project/domain.md`
  (Portal composes in the UI). Not a defect.
- hexagonal/clean-code, pricing asked twice per homepage request (once for the whole catalogue to find the
  candidates, then again by `ProductArticleAssembler` for the ≤ 4 drawn ones), `GetProductSelectionUseCase.cs:28,35`:
  the plan accepted this under `## Open assumptions`, and both calls are in-process. It is not a defect of this
  change.
- clean-code, `HomePage.CardsInViewAsync(params int[] expected)` swallowing `TimeoutException`
  (`Pages/HomePage.cs:143-168`): this is documented. It waits for the expected state, then reports the actual one so the
  assertion names it. Not a blanket catch of production code.
- dca, twin checkpoint: .NET-first markup and CSS are covered by the approved exception `scenario.home.slider-*`
  (`../dca-sample-specification/exceptions.md`, cited in the story and the plan). Not a silent fork.
- Changed test `tests/DcaShop.UnitTests/Specification/SharedScenariosTest.cs:29,55-60`: the plan has no
  `## Changed tests` section, but the change is backed by the answered decision `product-slider-01` (answer a:
  "turns `\"` into `"` and `\\` into `\` before comparing"). The diff does exactly that and nothing else, so the
  change is authorised.
- product description: no surface outside `## Surfaces` (the homepage only; no REST/MCP), no new persistence
  and no client framework (inline plain script, as the layout's theme switch). The buttons are native and
  keyboard-operable, and `data-test` is on every addressed element. Nothing from `## Out of scope` is delivered
  (no add-to-cart on the card, no popularity, no personalisation).

## Criteria re-checked
- shows-discover-products-slider-below-hero: met. The test asserts the heading "Discover products", that the slider
  is the hero's next sibling, and the layout between the hero and `features` (`HomeSliderE2eTest.cs:15-24`).
- slider-holds-four-different-products: met. It asserts 4 cards, 4 distinct names, and that every name is in the
  catalogue (`HomeSliderE2eTest.cs:26-37`).
- products-are-drawn-anew-per-request: met. It asserts that at least one of 10 reloads differs from the noted set
  (`HomeSliderE2eTest.cs:39-59`).
- product-without-price-is-not-offered: met. 16 of 21 seeded products are unpriced, and over 10 requests every card
  is a priced product (`ProductSliderTest.cs:29-45`).
- shows-the-priced-products-there-are: met. It asserts exactly 2 cards with exactly the 2 priced ids
  (`ProductSliderTest.cs:47-60`).
- card-shows-image-name-and-price: met. The image is loaded, and name, price and image equal the product page's
  (`HomeSliderE2eTest.cs:61-76`).
- card-links-to-product-page: met. Following the link shows `/products/{id}` with that card's name as the title
  (`HomeSliderE2eTest.cs:78-91`).
- desktop-shows-four-cards-side-by-side: met. All 4 cards are in view, with the same top and increasing left, and
  both buttons are disabled (`HomeSliderE2eTest.cs:93-105`).
- phone-shows-one-card-at-a-time: met. Exactly `[0]` is in view and "Previous" is disabled
  (`HomeSliderPhoneE2eTest.cs:19-26`).
- next-brings-the-following-card-into-view: met. It goes from `[0]` to exactly `[1]` (`HomeSliderPhoneE2eTest.cs:28-37`).
- previous-brings-the-preceding-card-into-view: met. It goes from `[1]` back to exactly `[0]`
  (`HomeSliderPhoneE2eTest.cs:39-49`).
- next-is-operable-by-keyboard: met. Tab moves the focus to "Next", then Enter moves from `[0]` to `[1]`
  (`HomeSliderPhoneE2eTest.cs:51-61`).
- next-is-disabled-at-the-last-card: met. After three presses `[3]` is in view and "Next" is disabled
  (`HomeSliderPhoneE2eTest.cs:63-76`).
- slider-does-not-move-by-itself: met. After 10 s the first card is still in view and the second is not
  (`HomeSliderPhoneE2eTest.cs:78-89`).
- Guard (no key, story Assumptions line 2): when nothing is priced, no `product-slider` section is rendered
  (`ProductSliderTest.cs:62-72`, `ProductSliderViewComponent.cs:24-27`).
