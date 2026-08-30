using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.Checkout;

public sealed class TaxCalculatorTest
{
    private readonly TaxCalculator _calculator = new();

    [Fact]
    public void ExtractsTheTaxContainedInAGrossAmountInsteadOfAddingIt() =>
        Assert.Equal(Money.Euro(19.00m), _calculator.ContainedTax(Money.Euro(119.00m)));

    [Fact]
    public void ContainedTaxAndNetAddUpToTheGrossAmount()
    {
        var gross = Money.Euro(249.95m);

        var tax = _calculator.ContainedTax(gross);

        Assert.Equal(gross, gross.Subtract(tax).Add(tax));
    }

    [Fact]
    public void ZeroContainsNoTax() =>
        Assert.Equal(Money.Euro(0m), _calculator.ContainedTax(Money.Euro(0m)));

    [Fact]
    public void HonoursAnExplicitRate() =>
        Assert.Equal(Money.Euro(7.00m), _calculator.ContainedTax(Money.Euro(107.00m), 0.07m));

    [Fact]
    public void RejectsANegativeRate() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => _calculator.ContainedTax(Money.Euro(100m), -0.19m));
}
