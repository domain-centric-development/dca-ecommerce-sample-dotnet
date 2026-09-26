# Judge — pay-through-the-provider

## Verdict
verdict: pass

This is a repeat round after decision `pay-through-the-provider-02`, which is answered and applied. The previous
round's one major finding is fixed at its source:
- The specification now carries `scenario.checkout.provider-authorizes-payment`.
- The browser test has that scenario's title.
- The Java twin carries a test under the same title.

The build round fixed both minor findings. No blocking defect is left, and all three criteria are met by behaviour.

## Perspectives covered
- ddd: `review-ddd` skill (profile `review.ddd`)
- hexagonal: `review-hexagonal` skill (profile `review.hexagonal`)
- clean-code: `review-clean-code` skill (profile `review.clean-code`)
- dca (added, `reviews: dca`): `dca-review` skill (profile `review.dca`)

Knowledge source: `dca-knowledge` is named in the profile. No finding in this round needed a catalog answer. The
plan's citation `rule/contextmap/dca-map-008.md` (upstream contract types stay inside the matching adapter) still
holds: `PaymentRequest` and `PaymentAuthorization` are private records inside `RestPaymentProvider`
(`src/DcaShop.Checkout/Adapter/Outgoing/Payment/RestPaymentProvider.cs:94-96`).

## Confirmed defects
| Perspective | File:line | Severity | Defect | Fix |
| --- | --- | --- | --- | --- |
| — | — | — | none | — |

## Considered and dropped
- **ddd:** the glossary entry `PaymentProviderNotFound · PaymentProviderUnavailable · PaymentInitiationFailed`
  (`src/DcaShop.Checkout/Domain/glossary.md:492-495`) still says "The provider's own reason travels through
  unchanged". This is not a code defect. The reason still travels on the exception and into the log. The plan's
  `## Glossary proposals` hand the new wording to the document stage, which runs next.
- **clean-code:** the method name `APaymentTheProviderAuthorizesLeadsOnToTheReview`
  (`tests/DcaShop.E2eTests/ProviderPaymentE2eTest.cs:24`) no longer matches its new `DisplayName`. The test stage
  kept it on purpose: the gate's criterion table and its red record name the method. The shared-scenario binding
  goes by the `DisplayName`, which matches. This is naming preference, not a defect.
- **clean-code:** after the constructor was made private, `SubmitPaymentUseCase` still checks
  `initiation.ProviderReference is not { } providerReference`. The factories now make that case impossible. The check
  is still the pattern that binds `providerReference`, so it is not dead code. It is harmless and not worth a round.
- **clean-code:** both `RestPaymentProvider.Unavailable` and `SubmitPaymentUseCase` log an unavailable payment. This
  was dropped in the previous round and still is: the adapter logs the transport detail, the use case logs the
  provider id.
- **hexagonal:** `CheckoutPageController.CustomerMessage` switches on the exception type. It is the context's one
  failure translation site, and deciding what the customer is told belongs there.
- **hexagonal:** the remote call stays before `ITransactionBoundary.InTransactionAsync` (ADR-004), and
  `Application/Shared/IPaymentProvider.cs` imports no framework type. No finding.
- **dca (twin checkpoint):** the Java suite has `ProviderPaymentE2ETest` with
  `@DisplayName("A payment the provider authorizes moves the checkout on to the review step")`. The two suites bind
  the same scenario, so they do not fork silently.
- **Test levels:** the happy path is the only browser test. The changed adapter is exercised by six integration tests
  against a WireMock.Net stub (`tests/DcaShop.IntegrationTests/ProviderPaymentTest.cs`). Only the use-case unit tests
  replace the port with a test double. No finding.
- **Product description:** `## Not part of the product` names "Real payment". The shipped shop configures no
  `Checkout:PaymentProvider:BaseUrl`, so it still pays with the stand-in. The REST integration is the one
  `project/tech.md` `## Integrations` lists. Unchanged from the previous round.
- **Architecture suite:** everything it enforces was left to the gate (129/129 passed).

## Criteria re-checked
- **authorized-payment-moves-to-review: met.**
  - Given: the test reaches the payment step through the pages and arranges `PaymentProviderStub.AuthorizesPayments()`.
  - When: it pays through the page.
  - Then: it asserts `review.IsOnPage`.
  - And: it asserts exactly one `POST /payments` whose amount and currency equal the session total shown on the
    payment page (`ProviderPaymentE2eTest.cs:46-50`).
- **refused-payment-stays-at-payment: met.** A `402` stub gets a `200` answer with the payment page, not a redirect.
  `payment-error-message` holds exactly "The payment was refused. Please choose another way to pay."
  (`ProviderPaymentTest.cs:30`, through the shared `AssertStaysAtPaymentShowingAsync`).
- **slow-provider-counts-as-unavailable: met.** A stub that answers after 5 s hits the 2 s client timeout
  (`PaymentProviderOptions.Timeout`), and the page shows "The payment provider is not available right now. Please try
  again later." The test also requires the answer in under 4 s, so the late answer is abandoned, not awaited
  (`ProviderPaymentTest.cs:41-52`).
- **Out of scope** (webhook, refunds, cancellations): nothing is delivered. `CancelPaymentAsync` makes no call.

## Previous round
- **major**, `tests/DcaShop.E2eTests/ProviderPaymentE2eTest.cs` — the browser test had no scenario in the shared
  specification. **Fixed.** Decision `pay-through-the-provider-02` was answered with option a.
  `dca-sample-specification/scenarios.md:35-42` now has `scenario.checkout.provider-authorizes-payment`, titled "A
  payment the provider authorizes moves the checkout on to the review step", and its steps match the story's
  criterion. The test's `DisplayName` is that title (`ProviderPaymentE2eTest.cs:23`).
- **minor**, `RestPaymentProvider.cs` `CancelPaymentAsync` — it answered `Refused` for an operation the provider does
  not offer, and the port's summary did not allow for that. **Fixed.** `PaymentResult.Refused` now documents "The
  provider declined the operation, or does not offer it at all"
  (`src/DcaShop.Checkout/Application/Shared/IPaymentProvider.cs:69-73`), so the answer is within the contract.
- **minor**, `IPaymentProvider.cs` — the public positional constructor of `PaymentResult` let a caller build
  inconsistent results. **Fixed.** `PaymentResult` is now a non-positional record with a private constructor
  (`IPaymentProvider.cs:50-52`). Only `Succeeded`, `Refused` and `Unavailable` create one, and `Success` is derived
  from `Outcome`.
