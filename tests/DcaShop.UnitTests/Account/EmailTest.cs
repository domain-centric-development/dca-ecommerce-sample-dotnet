using DcaShop.Account.Domain.Model;

namespace DcaShop.UnitTests.Account;

public sealed class EmailTest
{
    [Fact]
    public void AnAddressIsNormalizedToLowerCase() =>
        Assert.Equal("jane@example.com", Email.Of("  Jane@Example.COM ").Value);

    [Fact]
    public void TwoSpellingsOfTheSameAddressAreTheSameValue() =>
        Assert.Equal(Email.Of("jane@example.com"), Email.Of("JANE@EXAMPLE.COM"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-address")]
    [InlineData("no@domain")]
    [InlineData("@example.com")]
    public void AMalformedAddressIsRefused(string value) => Assert.Throws<ArgumentException>(() => Email.Of(value));

    [Fact]
    public void TheAddressSplitsIntoItsTwoHalves()
    {
        var email = Email.Of("jane.doe@example.com");

        Assert.Equal("jane.doe", email.LocalPart());
        Assert.Equal("example.com", email.Domain());
    }
}
