using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.UnitTests.SharedKernel;

/// <summary>A currency is named by an ISO 4217 code; two currencies are equal exactly when their codes are.</summary>
public sealed class CurrencyTest
{
    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("JPY")]
    public void AnIsoCodeNamesItsCurrency(string code) => Assert.Equal(code, Currency.Of(code).Code);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("eur")]
    [InlineData("EURO")]
    [InlineData("ABC")]
    public void ACodeIsoDoesNotKnowIsRefused(string code)
    {
        var refusal = Assert.Throws<ArgumentException>(() => Currency.Of(code));
        Assert.StartsWith("Currency must be an ISO 4217 code", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TwoCurrenciesWithTheSameCodeAreEqual()
    {
        Assert.Equal(Currency.Euro, Currency.Of("EUR"));
        Assert.NotEqual(Currency.Euro, Currency.Of("USD"));
    }

    [Fact]
    public void ACurrencyPrintsItsCode() => Assert.Equal("EUR", Currency.Euro.ToString());

    [Fact]
    public void AmountsInTwoCurrenciesHaveNoSum()
    {
        var mismatch = Assert.Throws<CurrencyMismatchException>(
            () => Money.Euro(1m).Add(Money.Of(1m, Currency.Of("USD"))));

        Assert.Equal("Cannot add money in EUR and USD", mismatch.Message);
        Assert.Equal(Currency.Euro, mismatch.Left);
        Assert.Equal(Currency.Of("USD"), mismatch.Right);
    }
}