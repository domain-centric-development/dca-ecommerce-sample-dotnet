# Tests — pay-through-the-provider

Carried in-session with the profile's `carrier.test: e2e-testing` skill applied to the browser test. `dca-knowledge` is
the profile's knowledge source. No decision in this stage rested on a catalog node.

Decision `pay-through-the-provider-01` is answered. The end-user suite starts the shop itself, and the payment
provider's stub belongs to the suite: it starts with the shop, the shop's provider address points at it, and each
test arranges the stub's answers. The story no longer names 20.00 EUR. The provider receives one payment request for
the checkout session's total.

Decision `pay-through-the-provider-02` is answered (option a). The specification now has
`scenario.checkout.provider-authorizes-payment`, titled "A payment the provider authorizes moves the checkout on to the
review step". This rerun renamed the browser test's `DisplayName` to that title, which is the only change it made. The
method name stays as it was, so the table row and the red record the gate kept from the first run still match.

<!-- gate:tests -->
| criterion | test |
| --- | --- |
| authorized-payment-moves-to-review | DcaShop.E2eTests.ProviderPaymentE2eTest#APaymentTheProviderAuthorizesLeadsOnToTheReview |
| refused-payment-stays-at-payment | DcaShop.IntegrationTests.ProviderPaymentTest#ARefusedPaymentKeepsTheCustomerAtThePaymentStepWithTheRefusalText |
| slow-provider-counts-as-unavailable | DcaShop.IntegrationTests.ProviderPaymentTest#AProviderThatAnswersOnlyAfterFiveSecondsCountsAsUnavailable |

## Files
- tests/DcaShop.E2eTests/ProviderPaymentE2eTest.cs
- tests/DcaShop.E2eTests/PaymentProviderStub.cs
- tests/DcaShop.E2eTests/ShopUnderTest.cs
- tests/DcaShop.E2eTests/Pages/PaymentPage.cs
- tests/DcaShop.E2eTests/Pages/ReviewPage.cs
- tests/DcaShop.IntegrationTests/ProviderPaymentTest.cs
- tests/DcaShop.UnitTests/Checkout/SubmitPaymentUseCaseTest.cs
- src/DcaShop.Checkout/Application/Shared/IPaymentProvider.cs
- src/DcaShop.Web/Views/Checkout/_OrderSummary.cshtml

## Notes
- ProviderPaymentE2eTest#APaymentTheProviderAuthorizesLeadsOnToTheReview fails on
  `Assert.Single(PaymentProviderStub.PaymentRequests())` with "the collection was empty". The shop does not read
  `Checkout:PaymentProvider:BaseUrl` yet, so the stand-in takes the payment and the review appears, but the stub
  never receives a request. The test reads the session total from the payment page's order summary and asserts
  exactly one `POST /payments` with that amount and currency.
- The suite's provider stub (`PaymentProviderStub`) is a WireMock.Net server on a free port, started with the shop.
  `ShopUnderTest` passes its address as `Checkout:PaymentProvider:BaseUrl`. By default the stub authorizes every
  payment (`201` with a reference), so the existing checkout tests (`CheckoutGuestE2eTest`, `CheckoutLoginE2eTest`,
  `CheckoutSnapshotE2eTest`) keep paying once the REST provider replaces the stand-in. None of their lines changed.
  They are green now.
- The new test class sits in the collection `payment provider` with `DisableParallelization = true`. It therefore runs
  alone, after the parallel classes, and the requests the stub records (reset by `AuthorizesPayments()`) are its own.
- A shop started elsewhere (`E2E_BASE_URL`) is not pointed at the stub, so there the new test fails. That follows the
  decision: the suite brings its own shop and dependencies.
- Selector added: `data-test="order-summary-total"` on the total in `Views/Checkout/_OrderSummary.cshtml`. It is an
  attribute only, with no behaviour. Sync duty: the Java sample's Pug order summary needs the same attribute, and the
  Java suite needs a test under the scenario's title (decision 02). Both are flagged for the document stage.
- Rerun after decision 02: the build stage has already delivered, so the renamed browser test is **green** (1/1).
  Its red run was recorded before the build (`.tests-red`), and this rerun changed no assertion. `dotnet format
  --verify-no-changes` is clean.
- New page-object queries: `PaymentPage.TotalAsync()` and `ReviewPage.IsOnPage`.
- ProviderPaymentTest#ARefusedPaymentKeepsTheCustomerAtThePaymentStepWithTheRefusalText fails on
  `Assert.Equal(OK, Found)`. The provider address is not read yet, so the stand-in takes the payment and the step
  redirects to the review.
- ProviderPaymentTest#AProviderThatAnswersOnlyAfterFiveSecondsCountsAsUnavailable fails on the same assertion, for the
  same reason. It also asserts that the answer arrives in under 4 s, so the 5 s answer must be abandoned at the 2 s
  timeout, not awaited.
- The scenarios' "stays at the payment step" is asserted as: the payment page itself answers (200, `payment-form`),
  not a redirect, and `payment-error-message` holds exactly the story's text (HTML-decoded).
- Adapter cases outside the table, in `ProviderPaymentTest`, all red for the same reason:
  - `AnAuthorizedPaymentMovesOnToTheReviewAfterOneRequestForTheSessionTotal`: 201 with a reference redirects to
    `/checkout/review` after exactly one `POST /payments` with amount 9.99 and currency EUR. It is red on
    `Assert.Single`.
  - `AProviderFailingWithAServerErrorCountsAsUnavailable` (500).
  - `AnAuthorizationWithoutAPaymentReferenceCountsAsUnavailable` (201 with `{}`).
  - `AProviderThatCannotBeReachedCountsAsUnavailable` (the stub is stopped before the payment).
  The request fields `amount`/`currency` and the answer's `reference` follow the plan's assumption.
- unit tests: `DcaShop.UnitTests.Checkout.SubmitPaymentUseCaseTest#APaymentTheProviderRefusesIsReportedAsARefusalAndLeavesTheSessionAtPayment`
  and `#AProviderThatCannotBeReachedIsReportedAsUnavailableAndLeavesTheSessionAtPayment` cover one invariant: a
  refused initiation raises `PaymentInitiationFailedException`, an unavailable one raises
  `PaymentProviderUnavailableException`, and neither changes the session (still at Payment, no `PaymentSelection`).
  Both fail on `Assert.ThrowsAsync`, because the stubbed factories throw `NotImplementedException`.
- Stubs: `IPaymentProvider.PaymentResult.Refused(string)` and `.Unavailable(string)` throw `NotImplementedException`.
- Runs: unit 2 red / 176 green, integration 6 red / 59 green, E2E 1 red / 37 green. Only this story's tests are red.
  `dotnet format --verify-no-changes` is clean.
