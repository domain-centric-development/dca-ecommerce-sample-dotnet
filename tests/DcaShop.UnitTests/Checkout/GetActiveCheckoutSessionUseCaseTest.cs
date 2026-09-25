using DcaShop.Checkout.Adapter.Outgoing.Persistence;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

/// <summary>
/// A session that is not there is the one step decision the snapshot cannot take — there is no snapshot to
/// ask. The use case answers it, and these cases live here because of that.
/// </summary>
public sealed class GetActiveCheckoutSessionUseCaseTest
{
    private readonly InMemoryCheckoutSessionRepository _sessions = new();

    [Theory]
    [InlineData(CheckoutStep.BuyerInfo)]
    [InlineData(CheckoutStep.Delivery)]
    [InlineData(CheckoutStep.Payment)]
    [InlineData(CheckoutStep.Review)]
    [InlineData(CheckoutStep.Confirmation)]
    public async Task WithoutSessionEveryStepSendsTheCustomerBackToTheCart(CheckoutStep step)
    {
        var result = await UseCase().ExecuteAsync(new GetActiveCheckoutSessionQuery("nobody", step));

        Assert.Null(result.Session);
        Assert.Equal(StepAccess.BackToCart(), result.StepAccess);
    }

    [Fact]
    public async Task WithoutARequestedStepThereIsNoAccessDecisionToTake()
    {
        var result = await UseCase().ExecuteAsync(new GetActiveCheckoutSessionQuery("nobody", null));

        Assert.Null(result.Session);
        Assert.Null(result.StepAccess);
    }

    private GetActiveCheckoutSessionUseCase UseCase() => new(_sessions);
}