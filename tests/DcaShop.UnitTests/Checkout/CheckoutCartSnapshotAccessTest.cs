using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

public sealed class CheckoutCartSnapshotAccessTest
{
    private static readonly ShippingOption Standard = new("standard", "Standard", "5-7 days", Money.Euro(4.99m));

    private static CheckoutSession Started()
    {
        var line = new CheckoutLineItem(CheckoutLineItemId.Generate(), ProductId.Generate(), "Thing", Money.Euro(10m), 2, null);
        return CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("guest"), new[] { line }, line.LineTotal);
    }

    private static CheckoutSession AtReview()
    {
        var session = Started();
        session.SubmitBuyerInfo(new BuyerInfo("a@b.de", "Ada", "Lovelace", "123"));
        session.SubmitDelivery(new DeliveryAddress("Street 1", "Town", "12345", "DE"), Standard);
        session.SubmitPayment(new PaymentSelection(PaymentProviderId.Of("invoice")));
        return session;
    }

    private static CheckoutSession Confirmed()
    {
        var session = AtReview();
        ConfirmWith(session, session.LineItems.ToDictionary(i => i.ProductId, i => new AlwaysAvailable().Resolve(i.ProductId)));
        return session;
    }

    private static CheckoutCartSnapshot Snapshot(CheckoutSession session) => CheckoutCartSnapshot.From(session);

    private sealed class AlwaysAvailable : ICheckoutArticlePriceResolver
    {
        public ArticlePrice Resolve(ProductId productId) => new(Money.Euro(10m), true, 99);
    }

    [Fact]
    public void FirstStepIsAlwaysAccessible() =>
        Assert.Equal(StepAccess.Grant(), Snapshot(Started()).AccessTo(CheckoutStep.BuyerInfo));

    [Theory]
    [InlineData(CheckoutStep.Delivery)]
    [InlineData(CheckoutStep.Payment)]
    [InlineData(CheckoutStep.Review)]
    public void SkippingAheadRedirectsToTheCurrentStep(CheckoutStep step) =>
        Assert.Equal(StepAccess.RedirectTo(CheckoutStep.BuyerInfo), Snapshot(Started()).AccessTo(step));

    [Fact]
    public void PrematureConfirmationRedirectsToTheCurrentStep() =>
        Assert.Equal(StepAccess.RedirectTo(CheckoutStep.BuyerInfo), Snapshot(Started()).AccessTo(CheckoutStep.Confirmation));

    [Fact]
    public void CompletedStepsCanBeVisitedAgain()
    {
        var snapshot = Snapshot(AtReview());

        Assert.Equal(StepAccess.Grant(), snapshot.AccessTo(CheckoutStep.BuyerInfo));
        Assert.Equal(StepAccess.Grant(), snapshot.AccessTo(CheckoutStep.Delivery));
        Assert.Equal(StepAccess.Grant(), snapshot.AccessTo(CheckoutStep.Payment));
        Assert.Equal(StepAccess.Grant(), snapshot.AccessTo(CheckoutStep.Review));
    }

    [Fact]
    public void ConfirmedSessionOnlyReachesTheConfirmation()
    {
        var snapshot = Snapshot(Confirmed());

        Assert.Equal(StepAccess.Grant(), snapshot.AccessTo(CheckoutStep.Confirmation));
        Assert.Equal(StepAccess.RedirectTo(CheckoutStep.Confirmation), snapshot.AccessTo(CheckoutStep.Payment));
    }

    [Fact]
    public void CompletedSessionOnlyReachesTheConfirmation()
    {
        var session = Confirmed();
        session.Complete("ORDER-1");
        var snapshot = Snapshot(session);

        Assert.Equal(StepAccess.Grant(), snapshot.AccessTo(CheckoutStep.Confirmation));
        Assert.Equal(StepAccess.RedirectTo(CheckoutStep.Confirmation), snapshot.AccessTo(CheckoutStep.BuyerInfo));
    }

    [Fact]
    public void AbandonedSessionStartsOverAtTheCart()
    {
        var session = Started();
        session.Abandon();

        Assert.Equal(StepAccess.BackToCart(), Snapshot(session).AccessTo(CheckoutStep.BuyerInfo));
    }

    [Fact]
    public void ExpiredSessionStartsOverAtTheCart()
    {
        var session = Started();
        session.Expire();

        Assert.Equal(StepAccess.BackToCart(), Snapshot(session).AccessTo(CheckoutStep.Confirmation));
    }

    [Fact]
    public void ADeniedAccessNamesTheStepTheSessionIsOn()
    {
        var access = Snapshot(Started()).AccessTo(CheckoutStep.Review);

        Assert.False(access.Granted);
        Assert.False(access.IsBackToCart);
        Assert.Equal(CheckoutStep.BuyerInfo, access.RedirectStep);
    }
    /// <summary>What the use case does: the pricing judges, the session confirms.</summary>
    private static void ConfirmWith(CheckoutSession session, IReadOnlyDictionary<ProductId, ArticlePrice> facts)
    {
        var pricing = new CheckoutPricing();
        session.Confirm(pricing.ValidateItems(session, facts), pricing.CalculateOrderTotal(session, facts));
    }
}
