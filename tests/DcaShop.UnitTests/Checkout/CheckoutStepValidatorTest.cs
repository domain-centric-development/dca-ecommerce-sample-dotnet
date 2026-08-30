using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

public sealed class CheckoutStepValidatorTest
{
    private static readonly ShippingOption Standard = new("standard", "Standard", "5-7 days", Money.Euro(4.99m));

    private readonly CheckoutStepValidator _validator = new();

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
        session.Confirm(new AlwaysAvailable());
        return session;
    }

    private static CheckoutCartSnapshot Snapshot(CheckoutSession session) => CheckoutCartSnapshot.From(session);

    private sealed class AlwaysAvailable : ICheckoutArticlePriceResolver
    {
        public ArticlePrice Resolve(ProductId productId) => new(Money.Euro(10m), true, 99);
    }

    [Theory]
    [InlineData(CheckoutStep.BuyerInfo)]
    [InlineData(CheckoutStep.Delivery)]
    [InlineData(CheckoutStep.Payment)]
    [InlineData(CheckoutStep.Review)]
    [InlineData(CheckoutStep.Confirmation)]
    public void WithoutSessionEveryStepRedirectsToTheCart(CheckoutStep step) =>
        Assert.Equal("/cart", _validator.ValidateStepAccess(null, step));

    [Fact]
    public void FirstStepIsAlwaysAccessible() =>
        Assert.Null(_validator.ValidateStepAccess(Snapshot(Started()), CheckoutStep.BuyerInfo));

    [Theory]
    [InlineData(CheckoutStep.Delivery)]
    [InlineData(CheckoutStep.Payment)]
    [InlineData(CheckoutStep.Review)]
    public void SkippingAheadRedirectsToTheCurrentStep(CheckoutStep step) =>
        Assert.Equal("/checkout/buyer", _validator.ValidateStepAccess(Snapshot(Started()), step));

    [Fact]
    public void PrematureConfirmationRedirectsToTheCurrentStep() =>
        Assert.Equal("/checkout/buyer", _validator.ValidateStepAccess(Snapshot(Started()), CheckoutStep.Confirmation));

    [Fact]
    public void CompletedStepsCanBeVisitedAgain()
    {
        var snapshot = Snapshot(AtReview());

        Assert.Null(_validator.ValidateStepAccess(snapshot, CheckoutStep.BuyerInfo));
        Assert.Null(_validator.ValidateStepAccess(snapshot, CheckoutStep.Delivery));
        Assert.Null(_validator.ValidateStepAccess(snapshot, CheckoutStep.Payment));
        Assert.Null(_validator.ValidateStepAccess(snapshot, CheckoutStep.Review));
    }

    [Fact]
    public void ConfirmedSessionOnlyReachesTheConfirmation()
    {
        var snapshot = Snapshot(Confirmed());

        Assert.Null(_validator.ValidateStepAccess(snapshot, CheckoutStep.Confirmation));
        Assert.Equal("/checkout/confirmation", _validator.ValidateStepAccess(snapshot, CheckoutStep.Payment));
    }

    [Fact]
    public void CompletedSessionOnlyReachesTheConfirmation()
    {
        var session = Confirmed();
        session.Complete("ORDER-1");
        var snapshot = Snapshot(session);

        Assert.Null(_validator.ValidateStepAccess(snapshot, CheckoutStep.Confirmation));
        Assert.Equal("/checkout/confirmation", _validator.ValidateStepAccess(snapshot, CheckoutStep.BuyerInfo));
    }

    [Fact]
    public void AbandonedSessionStartsOverAtTheCart()
    {
        var session = Started();
        session.Abandon();

        Assert.Equal("/cart", _validator.ValidateStepAccess(Snapshot(session), CheckoutStep.BuyerInfo));
    }

    [Fact]
    public void ExpiredSessionStartsOverAtTheCart()
    {
        var session = Started();
        session.Expire();

        Assert.Equal("/cart", _validator.ValidateStepAccess(Snapshot(session), CheckoutStep.Confirmation));
    }

    [Fact]
    public void CurrentStepPathFollowsTheSessionsProgress()
    {
        Assert.Equal("/checkout/buyer", _validator.CurrentStepPath(Snapshot(Started())));
        Assert.Equal("/checkout/review", _validator.CurrentStepPath(Snapshot(AtReview())));
    }
}
