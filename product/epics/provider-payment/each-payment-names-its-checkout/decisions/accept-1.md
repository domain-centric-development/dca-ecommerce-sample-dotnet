---
id: each-payment-names-its-checkout-accept-1
story: each-payment-names-its-checkout
stage: document
kind: acceptance
asked: 2026-10-14T21:15:20Z
digest: 331239b5ce3eb76bcf04af9f34aa62c8b36fcc118c62cffe6fe82dcdc8d52e41
---

# Accept each-payment-names-its-checkout?

## Question
Every gate passed. Look at what the story delivers before it counts as delivered:

- the-payment-request-names-its-checkout: Given a checkout session at the payment step; And the payment provider authorizes payments; When the customer pays through the payment provider; Then the payment provider received one payment request for the checkout session's total; And that request carries the checkout reference, the checkout session's id as text, as `merchantReference` — `DcaShop.E2eTests.PaymentNamesItsCheckoutE2eTest#ThePaymentRequestCarriesTheCheckoutSessionAsItsMerchantReference`
- two-checkouts-send-two-references: Given two customers, each with a checkout session at the payment step; When both pay through the payment provider; Then the payment provider received two payment requests with two different `merchantReference` values — `DcaShop.IntegrationTests.PaymentReferenceTest#TwoCheckoutsSendTwoDifferentReferences`
- the-provider-answers-per-checkout: Given two customers, each with a checkout session at the payment step; And the payment provider refuses the payment for the first customer's checkout reference only; When both pay through the payment provider; Then the first customer stays at the payment step with "The payment was refused. Please choose another way to pay."; And the second customer's checkout shows the review step — `DcaShop.IntegrationTests.PaymentReferenceTest#TheProviderRefusingOneCheckoutLeavesTheOtherCheckoutPaid`
- a-second-attempt-names-the-same-checkout: Given a checkout session at the payment step; And the customer's first payment for it was refused by the payment provider; When the customer pays through the payment provider again; Then the payment provider received two payment requests for that checkout session; And both carry the same `merchantReference` — `DcaShop.IntegrationTests.PaymentReferenceTest#ASecondAttemptAfterARefusalNamesTheSameCheckout`

Start the application with `dotnet run --project src/DcaShop.Web`.

## Options
- accepted: the story is delivered.
- a correction: what should be different, written into the story (criteria and an `answered:` line naming this record); the story runs again from plan.

## Answer
answer: accepted
by: Christoph Bloemer
at: 2026-10-14T21:19:30Z
