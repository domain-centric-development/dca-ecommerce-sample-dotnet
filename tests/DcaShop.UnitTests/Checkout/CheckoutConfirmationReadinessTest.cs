using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

/// <summary>
/// What a session must be before "Place Order" reaches the stock: the preconditions of confirming, asked without
/// confirming.
/// </summary>
public sealed class CheckoutConfirmationReadinessTest
{
    private static readonly ShippingOption Standard = new("standard", "Standard", "5-7 days", Money.Euro(4.99m));

    private static CheckoutSession AtTheReview()
    {
        var line = new CheckoutLineItem(CheckoutLineItemId.Generate(), ProductId.Generate(), "Thing", Money.Euro(10m), 2, null);
        var session = CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("guest"), new[] { line }, line.LineTotal);
        session.SubmitBuyerInfo(new BuyerInfo("a@b.de", "Ada", "Lovelace", "123"));
        session.SubmitDelivery(new DeliveryAddress("Street 1", "Town", "12345", "DE"), Standard);
        session.SubmitPayment(new PaymentSelection(PaymentProviderId.Of("invoice")));
        session.ClearDomainEvents();
        return session;
    }

    /// <summary>The session as the store would hand it back in <paramref name="status"/>, on <paramref name="step"/>.</summary>
    private static CheckoutSession Stored(CheckoutSession session, CheckoutSessionStatus status, CheckoutStep step, string? orderReference = null) =>
        CheckoutSession.Reconstitute(
            session.Id,
            session.CartId,
            session.CustomerId,
            session.LineItems,
            session.Totals,
            step,
            status,
            session.BuyerInfo,
            session.DeliveryAddress,
            session.ShippingOption,
            session.PaymentSelection,
            orderReference);

    [Fact]
    public void ASessionNoLongerModifiableIsNotReady()
    {
        var session = Stored(AtTheReview(), CheckoutSessionStatus.Abandoned, CheckoutStep.Review);

        Assert.Throws<CheckoutNotModifiableException>(() => session.AssertReadyForConfirmation(CheckoutValidationResult.Valid()));
    }

    [Fact]
    public void ASessionWithAStepNotCompletedIsNotReady()
    {
        var line = new CheckoutLineItem(CheckoutLineItemId.Generate(), ProductId.Generate(), "Thing", Money.Euro(10m), 1, null);
        var session = CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("guest"), new[] { line }, line.LineTotal);
        session.SubmitBuyerInfo(new BuyerInfo("a@b.de", "Ada", "Lovelace", "123"));

        Assert.Throws<CheckoutStepNotCompletedException>(() => session.AssertReadyForConfirmation(CheckoutValidationResult.Valid()));
    }

    [Fact]
    public void ASessionNotAtTheReviewIsNotReady()
    {
        var session = Stored(AtTheReview(), CheckoutSessionStatus.Active, CheckoutStep.Delivery);

        Assert.Throws<CheckoutStepOutOfOrderException>(() => session.AssertReadyForConfirmation(CheckoutValidationResult.Valid()));
    }

    [Fact]
    public void AnInvalidValidationIsRefused()
    {
        var session = AtTheReview();
        var shortLine = ValidationError.InsufficientStock(session.LineItems[0].ProductId, 2, 1, session.LineItems[0].ProductName);

        var refused = Assert.Throws<CheckoutValidationException>(() =>
            session.AssertReadyForConfirmation(CheckoutValidationResult.WithErrors([shortLine])));

        Assert.Equal(shortLine, Assert.Single(refused.Validation.Errors));
    }

    [Fact]
    public void AskingChangesNothing()
    {
        var session = AtTheReview();

        session.AssertReadyForConfirmation(CheckoutValidationResult.Valid());

        Assert.Equal(CheckoutSessionStatus.Active, session.Status);
        Assert.Equal(CheckoutStep.Review, session.CurrentStep);
        Assert.Empty(session.DomainEvents);
    }
}