using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

/// <summary>When a payment the provider authorized already pays a checkout's total.</summary>
public sealed class PaymentSelectionTest
{
    private static readonly PaymentProviderId Provider = PaymentProviderId.Of("provider");

    [Fact]
    public void APaymentWithoutAReferenceCoversNothing()
    {
        var withReference = new PaymentSelection(Provider, "pay-1", Money.Euro(59.98m));
        var withoutReference = new PaymentSelection(Provider, null, Money.Euro(59.98m));

        Assert.True(withReference.Covers(Provider, Money.Euro(59.98m)));
        Assert.False(withoutReference.Covers(Provider, Money.Euro(59.98m)));
    }

    [Fact]
    public void APaymentCoversOnlyThroughTheProviderThatAuthorizedIt()
    {
        var payment = new PaymentSelection(Provider, "pay-1", Money.Euro(59.98m));

        Assert.True(payment.Covers(Provider, Money.Euro(59.98m)));
        Assert.False(payment.Covers(PaymentProviderId.Of("mock"), Money.Euro(59.98m)));
    }

    [Fact]
    public void APaymentCoversOnlyTheAmountItWasAuthorizedFor()
    {
        var payment = new PaymentSelection(Provider, "pay-1", Money.Euro(59.98m));

        Assert.True(payment.Covers(Provider, Money.Euro(59.98m)));
        Assert.False(payment.Covers(Provider, Money.Euro(64.98m)));
    }

    [Fact]
    public void AnAuthorizationKeepsTheChosenProviderAndCarriesItsReferenceAndAmount()
    {
        var chosen = new PaymentSelection(Provider);

        var authorized = chosen.Authorized("pay-1", Money.Euro(59.98m));

        Assert.Equal(new PaymentSelection(Provider, "pay-1", Money.Euro(59.98m)), authorized);
    }
}