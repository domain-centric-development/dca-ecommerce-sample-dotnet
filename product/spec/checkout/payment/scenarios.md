# checkout — payment

### Rule: A payment the provider authorizes moves the checkout on

#### authorized-payment-moves-to-review
- Given a checkout session at the payment step
- And the payment provider authorizes payments
- When the customer pays through the payment provider
- Then the checkout shows the review step
- And the payment provider received one payment request for the checkout session's total
Test: DcaShop.E2eTests.ProviderPaymentE2eTest#APaymentTheProviderAuthorizesLeadsOnToTheReview

### Rule: A refused payment keeps the customer at the payment step

#### refused-payment-stays-at-payment
- Given a checkout session at the payment step
- And the payment provider refuses payments
- When the customer pays through the payment provider
- Then the checkout stays at the payment step
- And the customer sees "The payment was refused. Please choose another way to pay."
Test: DcaShop.IntegrationTests.ProviderPaymentTest#ARefusedPaymentKeepsTheCustomerAtThePaymentStepWithTheRefusalText

### Rule: A provider that does not answer in time counts as unavailable

#### slow-provider-counts-as-unavailable
- Given a checkout session at the payment step
- And the payment provider answers only after 5 seconds
- When the customer pays through the payment provider
- Then the checkout stays at the payment step
- And the customer sees "The payment provider is not available right now. Please try again later."
Test: DcaShop.IntegrationTests.ProviderPaymentTest#AProviderThatAnswersOnlyAfterFiveSecondsCountsAsUnavailable

### Rule: The payment step offers the payment providers that are available

#### payment-step-offers-the-provider
- Given a guest who submitted the delivery step with "Standard Shipping"
- When the guest opens the payment step
- Then it shows under "Select Payment Method" exactly one choice, "Mock Payment (Test)"
- And a "Back" link and a "Continue to Review" button
Test: DcaShop.IntegrationTests.PaymentStepTest#ThePaymentStepOffersTheOneAvailableProvider

### Rule: The chosen shipping price is added to the order total

#### payment-step-totals-with-standard-shipping
- Given a guest who started the checkout with "Domain-Driven Design" once and chose "Standard Shipping"
- When the guest opens the payment step
- Then its "Order Summary" shows "Domain-Driven Design x1 54.99 EUR"
- And Subtotal "54.99 EUR", Shipping "4.99 EUR" and Total "59.98 EUR"
Test: DcaShop.IntegrationTests.PaymentStepTest#ThePaymentStepAddsStandardShippingToTheOrderTotal

#### payment-step-totals-with-express-shipping
- Given a guest who started the checkout with "Domain-Driven Design" once and chose "Express Shipping"
- When the guest opens the payment step
- Then it shows Subtotal "54.99 EUR", Shipping "9.99 EUR" and Total "64.98 EUR"
Test: DcaShop.IntegrationTests.PaymentStepTest#ThePaymentStepAddsExpressShippingToTheOrderTotal

### Rule: Choosing a provider starts the payment and moves on to the review

#### choosing-the-provider-opens-the-review
- Given a guest on the payment step
- When the guest chooses "Mock Payment (Test)" and presses "Continue to Review"
- Then the review step opens at `/checkout/review`
Test: DcaShop.E2eTests.ChooseHowToPayE2eTest#ChoosingTheMockProviderOpensTheReviewStep

#### unknown-provider-is-refused
- Given a guest on the payment step
- When the payment form is sent with the provider "paypal"
- Then the payment step opens again with the error "This way to pay is not offered. Please choose another one."
Test: DcaShop.IntegrationTests.PaymentStepTest#AProviderTheShopDoesNotOfferOpensThePaymentStepAgainWithTheRefusal
