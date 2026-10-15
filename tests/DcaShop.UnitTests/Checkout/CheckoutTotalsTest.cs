using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

public sealed class CheckoutTotalsTest
{
    [Fact]
    public void TheTotalIsTheSubtotalPlusTheShipping() =>
        Assert.Equal(Money.Euro(74.98m), CheckoutTotals.Calculate(Money.Euro(54.99m), Money.Euro(19.99m)).Total);

    [Fact]
    public void TheTaxIsContainedInSubtotalAndShippingNotAdded()
    {
        var totals = CheckoutTotals.Calculate(Money.Euro(100.00m), Money.Euro(19.00m));

        Assert.Equal(Money.Euro(19.00m), totals.Tax);
        Assert.Equal(Money.Euro(119.00m), totals.Total);
    }
}