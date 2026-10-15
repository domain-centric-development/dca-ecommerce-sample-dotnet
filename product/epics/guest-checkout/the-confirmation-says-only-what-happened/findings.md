---
story: the-confirmation-says-only-what-happened
epic: guest-checkout
---

# Findings — the-confirmation-says-only-what-happened

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.IntegrationTests/ConfirmationOrderTotalTest.cs:41 | minor | The order-total test looks for "Total Paid" in a part whose name suggests it never held the total. | `AvailabilityPages.Section(confirmation, "confirmation-message")` returns the markup from that element to the page's end (`ProductAvailabilityTest.cs:95`); the anchor adds nothing and borrows a helper named after product availability. | Assert `DoesNotContain("Total Paid", confirmation, …)` on the whole page. | open |
| 2 | ddd | tests/DcaShop.IntegrationTests/ConfirmationOrderTotalTest.cs:53 | minor | The order the test shows is completed without a buyer or payment, which the shop can never produce. | The stubbed snapshot is `CheckoutSessionStatus.Completed` with no buyer info, delivery address or payment selection; the adapter is proved on a state the `CheckoutSession` lifecycle cannot reach. | Give the fixture buyer info, address and payment selection as a completed session carries. | open |
