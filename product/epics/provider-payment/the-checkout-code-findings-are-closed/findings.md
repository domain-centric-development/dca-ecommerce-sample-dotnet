---
story: the-checkout-code-findings-are-closed
epic: provider-payment
classes: backfilled 2026-11-17 by request 20261117T220439032Z-classify-findings (4 rows, 0 without a fitting value)
---

# Findings — the-checkout-code-findings-are-closed

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | Origin | Outcome | In plain words | Defect | Fix | Status | Target | Type | Qualifier |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | plan | src/DcaShop.Checkout/Domain/Model/CheckoutSession.cs:143 | minor | introduced |  | The payment step still accepts a whole payment and turns away the kinds it must not record, instead of taking only the chosen provider. | The plan's `SubmitPayment(PaymentProviderId)` became a guard on `SubmitPayment(PaymentSelection)` throwing `ArgumentException`; the reason holds (a task changes no existing test, and two `Assert.Throws` lines pass a selection), the invariant is kept in the aggregate and pinned by `CheckoutPaymentStepTest`. The value object still admits the refused shape. | In a story that may touch those tests: take `PaymentProviderId`, build the selection inside, drop the guard and `CheckoutPaymentStepTest`. | open | code | interface | incorrect |
| 2 | ddd | product/spec/checkout/glossary.md:301 | minor | introduced |  | The glossary says the article facts come from an old lookup, not from where the code now gets them. | `domain-term:` `ArticlePrice` is defined as returned by `ICheckoutArticlePriceResolver` (legacy, :426-428) while `ICheckoutArticleDataPort.GetArticleFactsAsync` now produces it and the prose calls it "the article facts". | Name `GetArticleFactsAsync` as its source and "article facts" as its prose name in the `ArticlePrice` entry. | open | glossary | domain-term | incorrect |
| 3 | clean-code | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutCompletion/CheckoutStepPage.cs:207 | minor | introduced |  | Two refusals are handled in their own branches though they are handled exactly like the rest. | With the count gone, the `StockRanShort` and `PriceChangedSinceReview` cases differ from `default` only in the notice chosen; `Leave` already clears the product when none is given. | Map both kinds in `ReviewNoticeFor` and make the body one `Leave(Review, ReviewNoticeFor(refusal), refusal.ProductName)`. | open | code | duplication | extraneous |
| 4 | clean-code | tests/DcaShop.UnitTests/Checkout/CheckoutPricingTest.cs:11 | minor | introduced |  | The availability check's tests are still filed under the old name. | Class `CheckoutPricingTest` tests `CheckoutAvailabilityCheck`; kept on purpose, a task moves no test. | Rename to `CheckoutAvailabilityCheckTest` in the checkout tests clean-up. | open | test | naming | incorrect |
