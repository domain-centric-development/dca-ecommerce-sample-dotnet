---
id: pay-through-the-provider-01
story: pay-through-the-provider
stage: test
asked: 2026-09-25T22:10:25Z
---

# The browser test for an authorized payment needs a shop that pays through the test's stub, and a 20.00 EUR total

## Question
The plan puts `authorized-payment-moves-to-review` in the end-user suite (`tests/DcaShop.E2eTests`, Playwright), with the
provider as a WireMock.Net stub. That suite drives a shop **someone else started** (`E2E_BASE_URL`,
`tests/DcaShop.E2eTests/BaseE2eTest.cs`). It cannot start the shop itself, because referencing `DcaShop.Web` would be a
build-file change. Three things stand in the way, and none of them is the test stage's to settle:

1. **The shop has to talk to the test's stub.** It only does that when it is started with
   `Checkout__PaymentProvider__BaseUrl=<stub address>`. The test does not control that address. The plan proposes an
   `E2E_PAYMENT_PROVIDER_URL` and skipping without it, but the story gate refuses a skipped test ("was skipped rather
   than run"). So the test only proves anything where the worker environment starts the shop that way.
2. **The other checkout browser tests break against such a shop.** When the address is set, the REST provider
   *replaces* the stand-in (plan, `## Open assumptions`). `CheckoutGuestE2eTest`, `CheckoutLoginE2eTest` and
   `CheckoutSnapshotE2eTest` then pay through a provider that is only reachable while this one test's stub is running.
   Outside that window they are refused as unavailable. The test stage may not change them, because the plan lists no
   `## Changed tests`.
3. **No arrangement reaches 20.00 EUR.** Every seeded price and every shipping cost ends in .99
   (`SampleDataSeeder.cs:25-49`, `ShippingOptions.cs`), so no basket of seeded products sums to 20.00. A product at
   20.00 can only be created through `POST /api/products`, which is staff-only. Nothing grants the staff role
   (ADR-007: an operator token is minted out of band), and a browser test against a running shop has no way to mint
   one.

The refusal, the timeout and the adapter's other answers (201 with a reference, 5xx, 201 without a reference, no
connection) are covered in `tests/DcaShop.IntegrationTests/ProviderPaymentTest.cs`. The integration tests start the
shop in-process with the stub's address, and all of them are red for the right reason. Only the browser test for the
happy path is open.

## Options
- a: **A module-wide provider stub plus two environment variables.** The E2E assembly starts one WireMock.Net server
  at `E2E_PAYMENT_PROVIDER_URL` for the whole run, and it authorizes every `POST /payments` by default, so the existing
  checkout tests keep paying. The environment that runs the suite starts the shop with
  `Checkout__PaymentProvider__BaseUrl` set to the same address and supplies an out-of-band staff token as
  `E2E_STAFF_TOKEN`, which the test uses to create a product at 20.00. The test asserts exactly one request *for 20.00
  EUR* in the stub's log (the other suites buy other amounts, in parallel). Without the two variables the test fails
  instead of skipping. The worker environment and CI (`.github/workflows/ci.yml`) have to provide them.
- b: **Move the happy path to the integration level.** `ProviderPaymentTest` already shows this: the shop starts
  in-process with the stub, the customer pays, and the answer is the redirect to the review step plus one request for
  the session total. The plan would name the happy path's level `integration (the running shop cannot be pointed at a
  per-test provider)`. The 20.00 EUR total would be arranged there by creating a 20.00 product with a staff token
  minted through `JwtTokenService`, as `ApiFlowTest` does. The story's happy-path mark would then have no browser test.
  That is a re-plan, and the gate's happy-path check has to accept it.
- c: **Offer the stand-in beside the REST provider** instead of replacing it, so a provider-configured shop still has
  "mock" for the existing tests. This changes the plan's design: the page offers two providers, and the page object
  would have to pick one by name.

## Recommendation
a — it keeps the happy path in the browser, as the story marks it, and leaves every existing test unchanged. The cost is
an environment contract: two variables and one start setting, the same kind of precondition as `E2E_BASE_URL` and
`E2E_EMBEDDED` already are. If the human does not want the E2E run to depend on a staff token, choose b.

## Answer
answer: The end-user suite starts the shop itself, in the test process and on a free port (done in this sample's e2e base since 2026-09-26). The payment provider's stub belongs to the suite: it starts with the shop, the shop's provider address points at it, and each test arranges the stub's answers. The story no longer names 20.00 EUR: the provider receives one payment request for the checkout session's total.
by: Christoph Bloemer
at: 2026-09-26T06:08:25Z
rationale: An end-user suite that drives a shop started beforehand makes the gate depend on whatever answers on that address; a suite that starts its own shop brings every dependency, the stub included.

## Applied
at: 2026-09-26T06:13:35Z
stage: test
