# Plan — pay-through-the-provider: Pay through the payment provider

Carried in-session; the profile names no `carrier.plan`. Knowledge source: `dca-knowledge` (named in
`.agents/factory/factory.profile.yaml`), nodes cited where they decided something.

## Context

**Checkout** (`src/DcaShop.Checkout/`). The designed map (`project/domain.md`, relationship table) names the
Payment Service Provider as Checkout's one external upstream: "ACL / REST (outbound) … behind the caller-owned
`IPaymentProviderRegistry` port; the sample ships `InMemoryPaymentProviderRegistry` and `MockPaymentProvider` in
place of a real gateway". The generated map (`docs/architecture/context-map.md`, diagram) agrees:
`Checkout -->|"ACL / REST"| ext_payment_service_provider`, from `[ExternalUpstream("Payment Service Provider",
Translation.AntiCorruptionLayer, Interaction.Outbound, Protocol = "REST", …)]` on `CheckoutContext`
(`src/DcaShop.Checkout/CheckoutContext.cs`). The two maps do not disagree. No new context and no new relationship:
the story makes the declared relationship real.

The actor (customer) already reaches the behaviour: `POST /checkout/payment` →
`CheckoutPageController.SubmitPayment` (`src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs:106-108`)
→ `ISubmitPaymentInputPort`. The error slot on the page exists: `data-test="payment-error-message"`
(`src/DcaShop.Web/Views/Checkout/Payment.cshtml:7`). No new surface.

The integration is the one `project/tech.md` `## Integrations` lists: `POST /payments` with amount and currency;
`201` with a payment reference authorizes, `402` refuses, no answer within 2 seconds counts as unavailable; without
a configured address the in-sample stand-in takes payments. Persistence stays in memory (`## Persistence`), the page
stays server-rendered (`## Frontend approach`).

What the code does today (evidence):
- `IPaymentProvider.PaymentResult(bool Success, string? ProviderReference, string? ErrorMessage)` knows success and
  failure only — no distinction between a refusal and an outage (`Application/Shared/IPaymentProvider.cs`, nested
  record).
- `SubmitPaymentUseCase.ExecuteAsync` checks `IsAvailableAsync` (→ `PaymentProviderUnavailableException`), then
  `InitiatePaymentAsync` outside the transaction; any failed result → `PaymentInitiationFailedException` carrying the
  provider's message (`Application/CheckoutCompletion/SubmitPayment/SubmitPaymentUseCase.cs:42-63`). On a failed
  transaction it releases the intent via `CancelPaymentAsync` (`:84-113`).
- The web adapter renders `e.Message` of any `UseCaseException`/`DomainException` on the same step
  (`CheckoutPageController.Submit`, `:148-168`, → `Page(step, …, e.Message)`).
- Wiring: `services.AddSingleton<IPaymentProvider, MockPaymentProvider>()`
  (`Infrastructure/CheckoutContextRegistration.cs:53`); `AddCheckoutContext()` takes no configuration
  (`:30`), called from `src/DcaShop.Infrastructure/DcaShopRegistration.cs:50`. Account and Backoffice already take
  `IConfiguration` (`AccountContextRegistration.cs:26`, `BackofficeContextRegistration.cs:18`) — the pattern to mirror.
- `DcaShop.Checkout.csproj` references `Microsoft.AspNetCore.App`, so `IHttpClientFactory`/`AddHttpClient` are
  available without a new package.

## Changes

| Element | Kind | Location | New or changed |
| --- | --- | --- | --- |
| `IPaymentProvider.PaymentResult` | output-port result (nested record) | `Application/Shared/IPaymentProvider.cs` | changed — states *why* a payment did not go through: refused vs. provider unavailable (e.g. `Refused(reason)` / `Unavailable(reason)` factories beside `Succeeded`) |
| `SubmitPaymentUseCase` | use case | `Application/CheckoutCompletion/SubmitPayment/` | changed — a refused initiation → `PaymentInitiationFailedException`; an unavailable one → `PaymentProviderUnavailableException`; still outside the transaction (ADR-004) and still no session change on either |
| `RestPaymentProvider` (name proposal) | outgoing adapter, `IPaymentProvider` implementation — the ACL towards the PSP | `Adapter/Outgoing/Payment/` | new — `POST {BaseUrl}/payments` with amount and currency; `201` + reference → succeeded; `402` → refused; timeout (2 s) or no connection → unavailable; provider JSON types private to the adapter |
| `PaymentProviderOptions` (name proposal) | adapter configuration | `Adapter/Outgoing/Payment/` | new — `BaseUrl` (optional), `Timeout` (default 2 s); section `Checkout:PaymentProvider` |
| `MockPaymentProvider` | outgoing adapter (stand-in) | `Adapter/Outgoing/Payment/MockPaymentProvider.cs` | changed only as far as `PaymentResult` changes (its outage answers `Unavailable`) |
| `CheckoutContextRegistration.AddCheckoutContext` | DI registration | `Infrastructure/CheckoutContextRegistration.cs` | changed — takes `IConfiguration`; registers `RestPaymentProvider` (typed `HttpClient`) when `BaseUrl` is set, `MockPaymentProvider` otherwise |
| `DcaShopRegistration.AddDcaShop` | host registration | `src/DcaShop.Infrastructure/DcaShopRegistration.cs:50` | changed — passes `configuration` to `AddCheckoutContext` |
| `CheckoutPageController.Submit` (payment step) | incoming web adapter — the failure translation site | `Adapter/Incoming/Web/CheckoutPageController.cs` | changed — renders `PaymentInitiationFailedException` as "The payment was refused. Please choose another way to pay." and `PaymentProviderUnavailableException` as "The payment provider is not available right now. Please try again later." |
| `[ExternalUpstream("Payment Service Provider", …)]` `Rationale` | context declaration | `CheckoutContext.cs` | changed — no longer "in place of a real gateway"; the architecture test regenerates `docs/architecture/context-map.md`, committed with the change |

Architecture: the domain is untouched (no framework type enters it); the port stays in `Application/Shared`, its
implementation in `Adapter/Outgoing/Payment`; the provider's contract types stay inside that adapter
(`rule/contextmap/dca-map-008.md` — "upstream contract types must stay inside the matching adapter"); one
aggregate (`CheckoutSession`) per transaction, the remote call before it. The address is configuration, never a
constant in the adapter (`recipe/test-a-scenario-integrated.md`, step 3).

## Acceptance criteria

- authorized-payment-moves-to-review: Given a checkout session at the payment step with a total of 20.00 EUR, and the
  payment provider authorizes payments (answers `POST /payments` with `201` and a payment reference), when the
  customer pays through the payment provider, then the checkout shows the review step, and the payment provider
  received exactly one payment request, for amount 20.00 and currency EUR.
  → level: **e2e** — Playwright in `tests/DcaShop.E2eTests` (`e2eTest:`), the provider a WireMock.Net stub
  (already referenced by `DcaShop.E2eTests.csproj`); **happy path** as the story marks it.
- refused-payment-stays-at-payment: Given a checkout session at the payment step, and the payment provider refuses
  payments (answers `402`), when the customer pays through the payment provider, then the checkout stays at the
  payment step, and the customer sees "The payment was refused. Please choose another way to pay."
  → level: **integration** — `tests/DcaShop.IntegrationTests` (`test.integration:`), `WebApplicationFactory<Program>`
  with `Checkout:PaymentProvider:BaseUrl` set to a WireMock.Net stub (`http.stub: wiremock-net`), driven through the
  HTTP surface `POST /checkout/payment` as `ShopFlowTest` does.
- slow-provider-counts-as-unavailable: Given a checkout session at the payment step, and the payment provider answers
  only after 5 seconds, when the customer pays through the payment provider, then the checkout stays at the payment
  step, and the customer sees "The payment provider is not available right now. Please try again later."
  → level: **integration** — same source set and stub, the answer arranged with a 5 s delay.

Details the story fixes, each covered by the criterion it belongs to:
- The two texts are shown verbatim, in the page's existing error slot `data-test="payment-error-message"` — part of
  refused-payment-stays-at-payment and slow-provider-counts-as-unavailable.
- "Stays at the payment step": the response is the payment page (`/checkout/payment`), not a redirect to
  `/checkout/review`; the session's current step remains Payment — same two criteria.
- The request carries the session total (20.00) and its currency (EUR), and is sent once — part of
  authorized-payment-moves-to-review.
- "Within 2 seconds" (`project/tech.md`): the 5 s answer is abandoned at the timeout, not awaited — observable in
  slow-provider-counts-as-unavailable as a response well under 5 s; the test stage may assert a bound.

## Files

- `src/DcaShop.Checkout/Application/Shared/IPaymentProvider.cs` — changes: `PaymentResult` distinguishes refused and unavailable
- `src/DcaShop.Checkout/Application/CheckoutCompletion/SubmitPayment/SubmitPaymentUseCase.cs` — changes: map the two failure kinds to the two existing exceptions
- `src/DcaShop.Checkout/Adapter/Outgoing/Payment/RestPaymentProvider.cs` — changes: new REST adapter
- `src/DcaShop.Checkout/Adapter/Outgoing/Payment/PaymentProviderOptions.cs` — changes: new options
- `src/DcaShop.Checkout/Adapter/Outgoing/Payment/MockPaymentProvider.cs` — changes: follow the `PaymentResult` change
- `src/DcaShop.Checkout/Infrastructure/CheckoutContextRegistration.cs` — changes: configuration-driven choice of provider
- `src/DcaShop.Infrastructure/DcaShopRegistration.cs` — changes: pass configuration
- `src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs` — changes: the two customer texts on the payment step
- `src/DcaShop.Checkout/CheckoutContext.cs` — changes: `[ExternalUpstream]` rationale
- `docs/architecture/context-map.md` — changes: regenerated by the architecture test
- `tests/DcaShop.E2eTests/` — changes: new happy-path test (stage test)
- `tests/DcaShop.IntegrationTests/` — changes: new tests for the refusal and the timeout (stage test)
- `tests/DcaShop.UnitTests/Checkout/SubmitPaymentUseCaseTest.cs` — changes: unit tests for refused vs. unavailable (stage test); its `RecordingPaymentProvider` follows the `PaymentResult` change
- `src/DcaShop.Account/Infrastructure/AccountContextRegistration.cs` — read: how a context registration takes `IConfiguration` and binds options
- `tests/DcaShop.IntegrationTests/HttpStubSmokeTest.cs` — read: the WireMock.Net stub as this project starts it
- `tests/DcaShop.IntegrationTests/ShopFlowTest.cs` — read: driving the checkout steps over HTTP (`Step(client, "/checkout/payment", …)`, `:58-60`)
- `tests/DcaShop.IntegrationTests/CookiePolicyTest.cs` — read: `WithWebHostBuilder(...UseSetting(...))` for a per-test configuration (`:87-93`)
- `tests/DcaShop.E2eTests/EmbeddedShopE2eTest.cs`, `tests/DcaShop.E2eTests/E2eFactAttribute.cs` — read: an E2E test that only runs against a shop started for it
- `tests/DcaShop.E2eTests/CheckoutGuestE2eTest.cs`, `tests/DcaShop.E2eTests/Pages/PaymentPage.cs` — read: the page objects for the checkout flow
- `src/DcaShop.Web/Views/Checkout/Payment.cshtml` — read: the error slot `payment-error-message`
- `src/DcaShop.Checkout/Application/Shared/ShippingOptions.cs` — read: shipping costs (Free Shipping at 0) for arranging a 20.00 EUR total

## Glossary proposals

- Payment authorization: the payment provider's acceptance of a payment, answered with the payment reference the
  session keeps in its `PaymentSelection`.
- Payment refusal: the payment provider declines the payment; the session stays at the payment step and the customer
  chooses another way to pay.
- (changed meaning) `PaymentProviderUnavailable`: now also covers a provider that does not answer within the timeout
  or cannot be reached, not only one that reports itself unavailable.
- (changed statement) `PaymentInitiationFailed`: the glossary says "the provider's own reason travels through
  unchanged"; the customer now sees the shop's fixed refusal text. The reason may still travel on the exception for
  the log — the document stage states which.

## Open assumptions

- answered (story): the contract is the one in `project/tech.md` `## Integrations`; without a configured address the
  stand-in stays, so every existing test (they pay with `providerId "mock"`, e.g. `ShopFlowTest.cs:60`,
  `CrossContextEventFlowTest.cs:158`, `CheckoutOwnershipTest.cs:77`) keeps running unchanged.
- The contract names no JSON field names. Assumed: request `{"amount": 20.00, "currency": "EUR"}`, `201` body
  `{"reference": "<payment reference>"}`. A real provider may name them differently; that is a change to the adapter only.
- Any answer other than `201`/`402` (e.g. `5xx`), and a `201` without a reference, counts as unavailable and is
  logged — the contract names only the three outcomes. Covered by adapter integration tests outside the criteria
  (`recipe/test-a-scenario-integrated.md`, step 7).
- The contract has no availability, confirmation or cancellation endpoint. So `RestPaymentProvider.IsAvailableAsync`
  answers `true` (availability is learned from the payment request); `ConfirmPaymentAsync` answers success without a
  call (the port allows it: "Providers that complete the payment on initiation may answer without doing anything");
  `CancelPaymentAsync` makes no call and answers failed, which `SubmitPaymentUseCase.CancelQuietlyAsync` logs as a
  warning. Cancellations through the provider are out of scope.
- A caller's cancelled request is not a timeout: the adapter reports unavailable only for its own deadline or a failed
  connection, and lets the caller's `OperationCanceledException` through (the existing unit test
  `AnIntentIsReleasedEvenWhenTheRequestWasCancelled` depends on that distinction).
- When the address is configured the REST provider **replaces** the stand-in (one provider on the page). Its id and
  display name are not specified; assumed id `provider`, display name "Payment provider". The E2E page object selects
  the first provider, so no test depends on the name.
- The E2E happy path needs a shop that talks to the test's stub: the test starts WireMock.Net on an address it takes
  from an environment variable (proposal `E2E_PAYMENT_PROVIDER_URL`) and runs only when that is set, the shop having
  been started with `Checkout__PaymentProvider__BaseUrl` pointing there — the precedent is `EmbeddedModeFact` in
  `EmbeddedShopE2eTest.cs`. Other checkout E2E tests run against a shop without that setting.
- The 20.00 EUR total is arranged by the test (a product at 20.00 with Free Shipping, or seed prices that sum to it);
  the plan does not fix which.
- Sync duty, not planned here: the Java sample does not get this change in this story; `planning/porting-status.md`
  and the Java twin need it, and the browser test's title needs a scenario in the shared specification's
  `scenarios.md` (`SharedScenariosTest` fails on a test without a scenario when run with `SpecificationPath`). The
  specification is the user's; flagged for the document stage and the human.
