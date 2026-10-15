using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

public sealed class ShippingOptionTest
{
    [Fact]
    public void IdNameAndEstimateAreRequiredAndTrimmed()
    {
        Assert.Throws<ArgumentException>(() => new ShippingOption("  ", "Overnight", "Next day", Money.Euro(19.99m)));
        Assert.Throws<ArgumentException>(() => new ShippingOption("overnight", "  ", "Next day", Money.Euro(19.99m)));
        Assert.Throws<ArgumentException>(() => new ShippingOption("overnight", "Overnight", "  ", Money.Euro(19.99m)));

        var option = new ShippingOption(" overnight ", " Overnight ", " Next day ", Money.Euro(19.99m));

        Assert.Equal(("overnight", "Overnight", "Next day"), (option.Id, option.Name, option.EstimatedDelivery));
    }

    [Fact]
    public void TheCostIsRequired() =>
        Assert.Throws<ArgumentNullException>(() => new ShippingOption("overnight", "Overnight", "Next day", null!));
}