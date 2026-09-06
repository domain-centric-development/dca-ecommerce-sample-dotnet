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
        return CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("guest"), new[] { line }, line.LineTotal, new TaxCalculator());
    }

    private static CheckoutSession AtReview()
    {
        var session = Started();
        session.SubmitBuyerInfo(new BuyerInfo("a@b.de", "Ada", "Lovelace", "123"));
        session.SubmitDelivery(new DeliveryAddress("Street 1", "Town", "12345", "DE"), Standard, new TaxCalculator());
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
        Assert.Equal(StepAccess.BackToCart(), _validator.AccessTo(null, step));

    [Fact]
    public void FirstStepIsAlwaysAccessible() =>
        Assert.Equal(StepAccess.Grant(), _validator.AccessTo(Snapshot(Started()), CheckoutStep.BuyerInfo));

    [Theory]
    [InlineData(CheckoutStep.Delivery)]
    [InlineData(CheckoutStep.Payment)]
    [InlineData(CheckoutStep.Review)]
    public void SkippingAheadRedirectsToTheCurrentStep(CheckoutStep step) =>
        Assert.Equal(StepAccess.RedirectTo(CheckoutStep.BuyerInfo), _validator.AccessTo(Snapshot(Started()), step));

    [Fact]
    public void PrematureConfirmationRedirectsToTheCurrentStep() =>
        Assert.Equal(StepAccess.RedirectTo(CheckoutStep.BuyerInfo), _validator.AccessTo(Snapshot(Started()), CheckoutStep.Confirmation));

    [Fact]
    public void CompletedStepsCanBeVisitedAgain()
    {
        var snapshot = Snapshot(AtReview());

        Assert.Equal(StepAccess.Grant(), _validator.AccessTo(snapshot, CheckoutStep.BuyerInfo));
        Assert.Equal(StepAccess.Grant(), _validator.AccessTo(snapshot, CheckoutStep.Delivery));
        Assert.Equal(StepAccess.Grant(), _validator.AccessTo(snapshot, CheckoutStep.Payment));
        Assert.Equal(StepAccess.Grant(), _validator.AccessTo(snapshot, CheckoutStep.Review));
    }

    [Fact]
    public void ConfirmedSessionOnlyReachesTheConfirmation()
    {
        var snapshot = Snapshot(Confirmed());

        Assert.Equal(StepAccess.Grant(), _validator.AccessTo(snapshot, CheckoutStep.Confirmation));
        Assert.Equal(StepAccess.RedirectTo(CheckoutStep.Confirmation), _validator.AccessTo(snapshot, CheckoutStep.Payment));
    }

    [Fact]
    public void CompletedSessionOnlyReachesTheConfirmation()
    {
        var session = Confirmed();
        session.Complete("ORDER-1");
        var snapshot = Snapshot(session);

        Assert.Equal(StepAccess.Grant(), _validator.AccessTo(snapshot, CheckoutStep.Confirmation));
        Assert.Equal(StepAccess.RedirectTo(CheckoutStep.Confirmation), _validator.AccessTo(snapshot, CheckoutStep.BuyerInfo));
    }

    [Fact]
    public void AbandonedSessionStartsOverAtTheCart()
    {
        var session = Started();
        session.Abandon();

        Assert.Equal(StepAccess.BackToCart(), _validator.AccessTo(Snapshot(session), CheckoutStep.BuyerInfo));
    }

    [Fact]
    public void ExpiredSessionStartsOverAtTheCart()
    {
        var session = Started();
        session.Expire();

        Assert.Equal(StepAccess.BackToCart(), _validator.AccessTo(Snapshot(session), CheckoutStep.Confirmation));
    }

    [Fact]
    public void ADeniedAccessNamesTheStepTheSessionIsOn()
    {
        var access = _validator.AccessTo(Snapshot(Started()), CheckoutStep.Review);

        Assert.False(access.Granted);
        Assert.False(access.IsBackToCart);
        Assert.Equal(CheckoutStep.BuyerInfo, access.RedirectStep);
    }
}
