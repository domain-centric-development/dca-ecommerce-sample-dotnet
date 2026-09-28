# Document — pay-through-the-provider

This stage ran in this session. The glossary went through `ubiquitous-language` (profile `carrier.glossary`). The
designed map went through `context-map` (profile `carrier.domain`). The knowledge source is `dca-knowledge` (profile
`knowledge`). No statement in this stage needed a catalog answer. The one rule behind the wording, "the provider's
request and answer shapes stay inside it", is the catalog node the plan cites for rule `DCA-MAP-008` (upstream contract types stay inside the matching adapter).

## Glossary
| Term | Context | Added or changed | Definition source |
| --- | --- | --- | --- |
| `PaymentOutcome` (`Succeeded` · `Refused` · `Unavailable`) | Checkout | added, `src/DcaShop.Checkout/Domain/glossary.md` (section Failures, after the payment failures) | Plan `## Glossary proposals` ("Payment authorization", "Payment refusal"), entered under the code's name. Checked against `src/DcaShop.Checkout/Application/Shared/IPaymentProvider.cs:39-76` (the enum and the factory summaries) |
| `PaymentProviderNotFound · PaymentProviderUnavailable · PaymentInitiationFailed` | Checkout | changed: unavailable now covers unreachable, too slow and off-contract, and the customer's two fixed texts are named. The provider's reason travels on the failure and into the log | Plan `## Glossary proposals` (both "changed" items). Checked against `src/DcaShop.Checkout/Application/CheckoutCompletion/SubmitPayment/SubmitPaymentUseCase.cs:55-66`, `src/DcaShop.Checkout/Application/Shared/PaymentInitiationFailedException.cs:14-15` (the reason is the message), `src/DcaShop.Checkout/Adapter/Outgoing/Payment/RestPaymentProvider.cs:42-78` and `src/DcaShop.Checkout/Adapter/Incoming/Web/CheckoutPageController.cs` (`CustomerMessage`) |
| `PaymentProviderId` | Checkout | changed: the shop registers exactly one provider, `provider` (REST, when `Checkout:PaymentProvider:BaseUrl` is set) or `mock` (the stand-in) | Checked against `src/DcaShop.Checkout/Adapter/Outgoing/Payment/RestPaymentProvider.cs:27`, `src/DcaShop.Checkout/Adapter/Outgoing/Payment/MockPaymentProvider.cs:13` and `src/DcaShop.Checkout/Infrastructure/CheckoutContextRegistration.cs` (`AddPaymentProvider`) |

## Documents updated
| File | What changed | Verified by |
| --- | --- | --- |
| src/DcaShop.Checkout/Domain/glossary.md | The three glossary rows above | Read `IPaymentProvider.cs`, `SubmitPaymentUseCase.cs`, `PaymentInitiationFailedException.cs`, `PaymentProviderUnavailableException.cs`, `RestPaymentProvider.cs`, `MockPaymentProvider.cs`, `CheckoutPageController.cs` (in the diff) |
| project/domain.md | Relationship row "Payment Service Provider → `Checkout`" (line 69): the translation is now `src/DcaShop.Checkout/Adapter/Outgoing/Payment/RestPaymentProvider.cs` with its contract, and `src/DcaShop.Checkout/Adapter/Outgoing/Payment/MockPaymentProvider.cs` is the stand-in without a configured address. It no longer says "in place of a real gateway". Design choice 2 (line 141) names `RestPaymentProvider` as the adapter. Pattern, translation kind and direction are unchanged, and no relationship was added | `ls src/DcaShop.Checkout/Adapter/Outgoing/Payment/` (both files exist). The contract matches `project/tech.md` `## Integrations`. `[ExternalUpstream("Payment Service Provider", Translation.AntiCorruptionLayer, Interaction.Outbound, Protocol = "REST", …)]` in `src/DcaShop.Checkout/CheckoutContext.cs` is unchanged apart from its rationale |
| README.md | `## Run`: one paragraph on the payment stand-in, the switch `Checkout:PaymentProvider:BaseUrl` (environment `Checkout__PaymentProvider__BaseUrl`), `POST /payments`, and `Checkout:PaymentProvider:Timeout` (default 2 s) | `src/DcaShop.Checkout/Adapter/Outgoing/Payment/PaymentProviderOptions.cs:9-18` (`SectionName`, `BaseUrl`, `Timeout = 2 s`), `CheckoutContextRegistration.cs` `AddPaymentProvider`, `RestPaymentProvider.cs:47` (`"payments"`) |
| docs/architecture/context-map.md | Nothing by hand. The architecture test regenerated it in the build stage from the changed `[ExternalUpstream]` rationale (one line, already in the diff) | build.md `## Checks`: ArchitectureTests 129/129, and the map was not stale |

## Not documented
- **`project/product.md`** (`## Qualities` "a stand-in in the sample", `## Not part of the product` "Real payment"): left as it is. The shipped shop configures no provider address, so both statements still hold. Whether "Real payment" should leave the "not part of the product" list is the product owner's call, not this stage's.
- **The plan's proposal labels**: "Payment authorization" and "Payment refusal" landed as the `Succeeded` and
  `Refused` values of the `PaymentOutcome` entry. The code has no type by either name, so neither became an entry of
  its own. "(changed meaning) `PaymentProviderUnavailable" and "(changed statement) `PaymentInitiationFailed" landed
  in the changed entry `PaymentProviderNotFound · PaymentProviderUnavailable · PaymentInitiationFailed`. None is open.
- **Java twin glossary and the parent directory's porting status** (sync duty, `AGENTS.md` `## Sync duty`): outside this repository and outside this stage. The judge found `ProviderPaymentE2ETest` in the Java sample under the same scenario title. Whether the Java Checkout glossary, the Java designed map and the porting status carry `PaymentOutcome` and the REST adapter was not checked here. That waits for a cross-project pass.
- **The JSON field names** (`amount`, `currency`, `reference`): an open assumption in the plan. The contract in `project/tech.md` does not name them. The README gives only the path, so it does not claim a field layout the provider has not confirmed.
- **No availability, confirmation or cancellation endpoint** for the REST provider (`RestPaymentProvider.cs:80-86`): this is described in the class's remarks. The story puts refunds and cancellations out of scope, so no reader document names it. An ADR would be the place once a later story adds the webhook or cancellations.
- **Test-suite changes** (`ShopUnderTest`, `PaymentProviderStub`, `BaseE2eTest`): the E2E section of `README.md` and `AGENTS.md` `## Build & test` already describe the suite starting the shop itself (commit `5c2bb69`, present in the diff). The provider stub is test-internal and needs no reader document.
