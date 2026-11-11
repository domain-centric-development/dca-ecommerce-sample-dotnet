using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

/// <summary>The payment step records a way to pay, never a payment the provider already authorized.</summary>
public sealed class CheckoutPaymentStepTest
{
    private static readonly PaymentProviderId Provider = PaymentProviderId.Of("provider");

    [Fact]
    public void ASelectionWithAPaymentReferenceIsRefusedAndNothingIsRecorded()
    {
        var session = AtThePaymentStep();

        Assert.Throws<ArgumentException>(() => session.SubmitPayment(new PaymentSelection(Provider, "pay-1")));

        Assert.Null(session.PaymentSelection);
        Assert.Equal(CheckoutStep.Payment, session.CurrentStep);
    }

    [Fact]
    public void ASelectionWithAnAuthorizedAmountIsRefusedAndNothingIsRecorded()
    {
        var session = AtThePaymentStep();

        Assert.Throws<ArgumentException>(() => session.SubmitPayment(new PaymentSelection(Provider, AuthorizedAmount: Money.Euro(24.99m))));

        Assert.Null(session.PaymentSelection);
        Assert.Equal(CheckoutStep.Payment, session.CurrentStep);
    }

    /// <summary>One line of 20.00 EUR, the buyer and delivery steps done: at the payment step.</summary>
    private static CheckoutSession AtThePaymentStep()
    {
        var line = new CheckoutLineItem(CheckoutLineItemId.Generate(), ProductId.Generate(), "Thing", Money.Euro(20m), 1, null);
        var session = CheckoutSession.Start(new CartId(Guid.NewGuid()), CustomerId.Of("guest"), new[] { line }, line.LineTotal);
        session.SubmitBuyerInfo(new BuyerInfo("a@b.de", "Ada", "Lovelace", "123"));
        session.SubmitDelivery(new DeliveryAddress("Street 1", "Town", "12345", "DE"), new ShippingOption("standard", "Standard", "5-7 days", Money.Euro(4.99m)));
        return session;
    }
}