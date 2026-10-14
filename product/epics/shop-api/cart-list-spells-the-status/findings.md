---
story: cart-list-spells-the-status
epic: shop-api
---

# Findings — cart-list-spells-the-status

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Defect | Fix | Status |
|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.IntegrationTests/CartApiTest.cs:214 | minor | `StaffToken`/`SubjectOf` copied again (ApiFlowTest:147, ProductCreationResourceTest:88, ApiProblemDetailTest:112) | One shared integration-test helper for minting a staff token and reading the subject | done |
| 2 | clean-code | src/DcaShop.Cart/Adapter/Incoming/Api/ShoppingCartDtoConverter.cs:42 | minor | Summary cites "the reference API", which a reader of this file cannot see | Say what holds here: upper-case, the same for the single cart and the list | done |
| 3 | dca | src/DcaShop.Cart/Application/Operations/GetAllCarts/GetAllCartsResult.cs:14 | minor | Result carries `CartStatus` where the twin's carries the spelled string; the three harness questions are not answered | Record the answer ("none" included) in the porting log and align the twin or name why not | done |
| 4 | ddd | product/epics/shop-api/cart-list-spells-the-status/story.md:6 | minor | `feature: shopping`, while `GetAllCarts` sits under `Application/Operations` | The person sets `feature: operations` | open |
