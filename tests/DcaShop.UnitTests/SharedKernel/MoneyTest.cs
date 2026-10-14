using System.Globalization;

using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.SharedKernel;

/// <summary>An amount is kept to the cent and rounded half-up, which shows only on exactly half a cent.</summary>
public sealed class MoneyTest
{
    [Theory]
    [InlineData("0.125", "0.13")]
    [InlineData("8.785", "8.79")]
    [InlineData("8.7849", "8.78")]
    public void AnAmountIsRoundedHalfUpToTheCent(string amount, string rounded)
    {
        Assert.Equal(Parse(rounded), Money.Euro(Parse(amount)).Amount);
    }

    private static decimal Parse(string amount) => decimal.Parse(amount, CultureInfo.InvariantCulture);
}