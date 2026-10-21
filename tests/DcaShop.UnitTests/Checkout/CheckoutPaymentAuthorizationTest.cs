using DcaShop.Checkout.Domain.Event;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

/// <summary>
/// The payment step only chooses how to pay; the order is confirmed with the payment its provider authorized at
/// "Place Order", for exactly the order's total.
/// </summary>
public sealed class CheckoutPaymentAuthorizationTest
{
    private static readonly PaymentProviderId Provider = PaymentProviderId.Of("provider");
    private static readonly ProductId Product = ProductId.Generate();
    private static readonly ShippingOption Standard = new("standard", "Standard", "5-7 days", Money.Euro(4.99m));
    private static readonly ShippingOption Free = new("free", "Free", "Tomorrow", Money.Euro(0m));

    [Fact]
    public void ChoosingHowToPayRecordsTheProviderWithoutAPaymentAndRaisesNoEvent()
    {
        var session = AtThePaymentStep(Money.Euro(10m), Standard);
        session.ClearDomainEvents();

        session.SubmitPayment(new PaymentSelection(Provider));

        Assert.Equal(Provider, session.PaymentSelection!.ProviderId);
        Assert.False(session.PaymentSelection.HasReference);
        Assert.Empty(session.DomainEvents);
    }

    [Fact]
    public void ChoosingHowToPayIsRefusedForAnOrderThatOwesNothing()
    {
        var session = AtThePaymentStep(Money.Euro(0m), Free);

        Assert.Throws<NothingToPayException>(() => session.SubmitPayment(new PaymentSelection(Provider)));
    }

    [Fact]
    public void AnOrderIsNotConfirmedWithAPaymentThatDoesNotCoverItsTotal()
    {
        var session = AtTheReview();
        session.ClearDomainEvents();
        var tooLittle = new PaymentSelection(Provider, "pay-1", Money.Euro(19.99m));

        Assert.Throws<PaymentDoesNotCoverTotalException>(() => Confirm(session, tooLittle));

        Assert.Equal(CheckoutSessionStatus.Active, session.Status);
        Assert.Equal(CheckoutStep.Review, session.CurrentStep);
        Assert.False(session.PaymentSelection!.HasReference);
        Assert.Empty(session.DomainEvents);
    }

    [Fact]
    public void AConfirmedOrderHoldsThePaymentItsProviderAuthorized()
    {
        var session = AtTheReview();
        var authorized = new PaymentSelection(Provider, "pay-1", Money.Euro(24.99m));

        Confirm(session, authorized);

        Assert.Equal(authorized, session.PaymentSelection);
    }

    [Fact]
    public void ConfirmingTheOrderPublishesTheAuthorizedPaymentBeforeTheConfirmation()
    {
        var session = AtTheReview();
        session.ClearDomainEvents();
        var authorized = new PaymentSelection(Provider, "pay-1", Money.Euro(24.99m));

        Confirm(session, authorized);

        Assert.Collection(
            session.DomainEvents,
            payment => Assert.Equal(authorized, Assert.IsType<PaymentSubmitted>(payment).Payment),
            confirmation => Assert.IsType<CheckoutConfirmed>(confirmation));
    }

    /// <summary>Two of a 10.00 EUR product, the buyer and delivery steps done: at the payment step with the given total.</summary>
    private static CheckoutSession AtThePaymentStep(Money unitPrice, ShippingOption shipping)
    {
        var line = new CheckoutLineItem(CheckoutLineItemId.Generate(), Product, "Thing", unitPrice, 2, null);
        var session = CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("guest"), new[] { line }, line.LineTotal);
        session.SubmitBuyerInfo(new BuyerInfo("a@b.de", "Ada", "Lovelace", "123"));
        session.SubmitDelivery(new DeliveryAddress("Street 1", "Town", "12345", "DE"), shipping);
        return session;
    }

    /// <summary>At the review with a total of 24.99 EUR, the provider chosen.</summary>
    private static CheckoutSession AtTheReview()
    {
        var session = AtThePaymentStep(Money.Euro(10m), Standard);
        session.SubmitPayment(new PaymentSelection(Provider));
        return session;
    }

    /// <summary>What "Place Order" does once the provider authorized: the pricing judges today's facts, the session confirms.</summary>
    private static void Confirm(CheckoutSession session, PaymentSelection authorizedPayment)
    {
        var pricing = new CheckoutPricing();
        var facts = session.LineItems.ToDictionary(i => i.ProductId, _ => new ArticlePrice(Money.Euro(10m), true, 5));
        session.Confirm(pricing.ValidateItems(session, facts), facts, session.FingerprintAt(facts), authorizedPayment);
    }
}