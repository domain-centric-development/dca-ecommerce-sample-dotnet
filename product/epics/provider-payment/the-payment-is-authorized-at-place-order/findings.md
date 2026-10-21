---
story: the-payment-is-authorized-at-place-order
epic: provider-payment
---

# Findings — the-payment-is-authorized-at-place-order

The judge's confirmed defects that did not block the story, kept for a later story or a person's look. The pipeline appends; a person sets `Status` to `done` or `wont-fix`.

| # | Perspective | File:line | Severity | In plain words | Defect | Fix | Status |
|---|---|---|---|---|---|---|---|
| 1 | ddd | src/DcaShop.Checkout/Application/CheckoutCompletion/ConfirmCheckout/ConfirmCheckoutUseCase.cs:85 | minor | An order lost to a race after the payment leaves the payment authorized at the provider. | `Confirm` can still refuse after `AuthorizePaymentAsync` (superseded session, `PaymentDoesNotCoverTotalException`); the catch releases only the stock. The story's out-of-scope premise does not hold. | A follow-up story cancels the authorization there through `CancelPaymentAsync`. | open |
| 2 | ddd | product/spec/checkout/glossary.md:608 | minor | The glossary says nothing can fail after the payment, which is not so. | The cancellation entry's reason contradicts ADR-013's "What holds today". | Word it as ADR-013 does: no cancellation is sent when the confirmation fails after the authorization. | open |
| 3 | ddd | src/DcaShop.Checkout/Domain/Model/CheckoutSession.cs:109 | minor | The payment step would record an authorized payment if a caller handed one in. | `SubmitPayment(PaymentSelection)` does not itself hold invariant 1 (no reference); only the one caller's argument does. | Take `PaymentProviderId` and build the selection inside. | open |
| 4 | hexagonal | src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutCompletion/CheckoutStepPage.cs:229 | minor | A way to pay withdrawn before Place Order is answered with "please try again", which cannot succeed. | `PaymentProviderNotFoundException` falls to `OrderNotPlaced`. | Map it to its own review notice. | open |
| 5 | clean-code | src/DcaShop.Checkout/Domain/Model/PaymentDoesNotCoverTotalException.cs:15 | minor | The refusal message can claim two equal amounts differ. | `Covers` also fails on provider or reference; the message names only amounts. | Word it after `Covers` as a whole. | open |
| 6 | clean-code | src/DcaShop.Checkout/Domain/Model/PaymentSelection.cs:15 | minor | A leftover way to set a payment reference is unused. | `WithReference` has no caller. | Remove it with its glossary mention. | open |
| 7 | clean-code | src/DcaShop.Web/Views/Checkout/Review.cshtml:46 | minor | The review still has room for a payment reference it can no longer show. | `p.HasReference` cannot hold at the review now. | Remove the branch, in both shops. | open |
| 8 | clean-code | tests/DcaShop.UnitTests/Checkout/CheckoutPaymentAuthorizationTest.cs:81 | minor | A test helper's comment names a price one caller does not use. | "a 10.00 EUR product" while the price is a parameter. | Word it after the parameter. | open |
| 9 | clean-code | tests/DcaShop.UnitTests/Specification/SharedScenariosTest.cs:107 | minor | The same scenario shape is spelled out four times. | Tuple `(Key, Title, Tests)` at :107, :164, :174, :176. | A private record `Scenario`. | open |
| 10 | clean-code | tests/DcaShop.IntegrationTests/PlaceOrderPaymentTest.cs:124 | minor | A test helper quietly sets up the shop other lines rely on. | `Checkouts()` assigns `_shop`, read at :109 via `_shop!`. | Expose the shop from `PortCheckouts`. | open |
