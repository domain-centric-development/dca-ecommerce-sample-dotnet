# Tidy — pay-through-the-provider

**Repeat round**, after the build stage's repeat round for decision `pay-through-the-provider-02`. That round
changed only `Application/Shared/IPaymentProvider.cs`: `PaymentResult` got a private constructor, and the `Refused`
summary was widened. Both changes answer judge findings and already read cleanly, so this round made no new move.
The one move from the first tidy round is still in the working tree, and it stays listed below so that the table
matches what changed.

This stage ran in this session. The profile names no `carrier.tidy`. The guard `carrier.guard: dca-discipline` was
applied:
- In this round the only file written is this one; no code file was written.
- The earlier move is in `Infrastructure/`, which is DI registration. There no layer rule applies, and no type
  crossed a layer.

## Moves
| File | Move | Why it reads better |
| --- | --- | --- |
| src/DcaShop.Checkout/Infrastructure/CheckoutContextRegistration.cs | (First round, unchanged.) The trailing-slash expression in the named `HttpClient` setup became a private `WithTrailingSlash(Uri)`, and the inline comment moved into its summary. | The client setup reads as two assignments at one level, and the reason for the slash sits on the method that adds it. The same URI comes out for every input, so behaviour is unchanged. |

## Left alone
- `IPaymentProvider.PaymentResult`, as the repeat build left it (private constructor, three factories, derived
  `Success`): it is already clean. A positional record would read shorter, but it would expose its constructor again,
  and the judge asked for that constructor to be closed.
- `SubmitPaymentUseCase` and `RestPaymentProvider` both log an unavailable payment. The build file explains why: the
  use case knows the provider id, the adapter knows the transport detail. Whether one log line is enough is a
  question for the judge.
- `SubmitPaymentUseCase` checks the outcome twice: first `Outcome == Unavailable`, then
  `!Success || ProviderReference is null`. A switch over `PaymentOutcome` would not be shorter. The second check also
  guards a success without a reference, and the private constructor now rules that case out only by convention of the
  factories, not by type. Left as it is.
- `RestPaymentProvider.CancelPaymentAsync` answers `Refused` for an operation the provider does not offer. The port
  now documents this on `Refused`. A distinct "not supported" outcome would change the port contract, so it is not
  tidying.
- `MockPaymentProvider`'s summary still says "every payment is approved". That remains true while the stand-in is
  available, and the availability switch has its own doc comment.
- Tests: none changed, as this stage requires.

## Checks
- dotnet build: succeeded, 0 errors
- dotnet test tests/DcaShop.UnitTests --logger trx: passed 178, skipped 4, failed 0
- dotnet test tests/DcaShop.IntegrationTests --logger trx: passed 65, skipped 1, failed 0
- dotnet test tests/DcaShop.ArchitectureTests: passed 129, failed 0
- dotnet test tests/DcaShop.E2eTests --logger trx: passed 38, skipped 1, failed 0
- dotnet format (formatFix), then dotnet format --verify-no-changes: clean
