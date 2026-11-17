---
story: the-confirmation-says-only-what-happened
epic: guest-checkout
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (2 rows, 0 without a fitting value)
---

# Findings — the-confirmation-says-only-what-happened

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | clean-code | tests/DcaShop.IntegrationTests/ConfirmationOrderTotalTest.cs:41 | minor | introduced |  | The order-total test looks for "Total Paid" in a part whose name suggests it never held the total. | `AvailabilityPages.Section(confirmation, "confirmation-message")` returns the markup from that element to the page's end (`ProductAvailabilityTest.cs:95`); the anchor adds nothing and borrows a helper named after product availability. | Assert `DoesNotContain("Total Paid", confirmation, …)` on the whole page. | open | test | checking | incorrect |
| 2 | ddd | tests/DcaShop.IntegrationTests/ConfirmationOrderTotalTest.cs:53 | minor | introduced |  | The order the test shows is completed without a buyer or payment, which the shop can never produce. | The stubbed snapshot is `CheckoutSessionStatus.Completed` with no buyer info, delivery address or payment selection; the adapter is proved on a state the `CheckoutSession` lifecycle cannot reach. | Give the fixture buyer info, address and payment selection as a completed session carries. | open | test | domain-invariant | incorrect |
