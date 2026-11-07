using DcaShop.Checkout.Adapter.Outgoing.Persistence;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

/// <summary>
/// A session that is not there is the one answer the snapshot cannot give — there is no snapshot to ask. The use case
/// answers it with its refusal, whether a step was requested or not; the step page turns it into the way back to the
/// cart.
/// </summary>
public sealed class GetCheckoutSessionUseCaseTest
{
    private readonly InMemoryCheckoutSessionRepository _sessions = new();

    [Theory]
    [InlineData(CheckoutStep.BuyerInfo)]
    [InlineData(CheckoutStep.Delivery)]
    [InlineData(CheckoutStep.Payment)]
    [InlineData(CheckoutStep.Review)]
    [InlineData(CheckoutStep.Confirmation)]
    public async Task EveryStepFindsNoCheckoutTheCustomerDoesNotHave(CheckoutStep step)
    {
        var sessionId = Guid.NewGuid();

        var result = await UseCase().ExecuteAsync(GetCheckoutSessionQuery.ForStep(sessionId, "nobody", step));

        Assert.Equal(sessionId, Assert.IsType<GetCheckoutSessionResult.CheckoutSessionNotFound>(result.Value).SessionId.Value);
    }

    [Fact]
    public async Task WithoutARequestedStepNoCheckoutIsFoundEither()
    {
        var result = await UseCase().ExecuteAsync(new GetCheckoutSessionQuery(Guid.NewGuid(), "nobody"));

        Assert.IsType<GetCheckoutSessionResult.CheckoutSessionNotFound>(result.Value);
    }

    private GetCheckoutSessionUseCase UseCase() => new(_sessions);
}