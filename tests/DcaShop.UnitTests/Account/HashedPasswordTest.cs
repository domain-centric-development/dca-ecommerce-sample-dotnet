using System.Text;
using DcaShop.Account.Domain.Model;

namespace DcaShop.UnitTests.Account;

public sealed class HashedPasswordTest
{
    private static readonly TestPasswordHasher Hasher = new();

    [Theory]
    [InlineData("Short1")]          // under the minimum length
    [InlineData("alllower123")]     // no uppercase
    [InlineData("ALLUPPER123")]     // no lowercase
    [InlineData("NoDigitsHere")]    // no digit
    public void ThePolicyRefusesAWeakPassword(string plaintext) =>
        Assert.Throws<ArgumentException>(() => HashedPassword.ValidatePasswordStrength(plaintext));

    [Fact]
    public void ThePolicyRefusesMoreThanSeventyTwoBytes()
    {
        // The bound is bytes, not characters, because that is what BCrypt imposes: 24 three-byte characters are
        // well under any character limit and still too long.
        var multiByte = string.Concat(Enumerable.Repeat("€", 24)) + "Aa1";
        Assert.True(Encoding.UTF8.GetByteCount(multiByte) > HashedPassword.MaxByteLength);

        Assert.Throws<ArgumentException>(() => HashedPassword.ValidatePasswordStrength(multiByte));
    }

    [Fact]
    public void AViolationReadsAsASentenceForTheUser()
    {
        var e = Assert.Throws<ArgumentException>(() => HashedPassword.ValidatePasswordStrength("alllower123"));

        Assert.Equal("Password must contain at least one uppercase letter", e.Message);
    }

    [Fact]
    public void APolicyAbidingPasswordIsHashed()
    {
        var password = HashedPassword.FromPlaintext("Secret123", Hasher);

        Assert.NotEqual("Secret123", password.Hash);
        Assert.True(password.Matches("Secret123", Hasher));
        Assert.False(password.Matches("Secret124", Hasher));
    }

    [Fact]
    public void TheHashNeverShowsUpInToString() =>
        Assert.Equal("HashedPassword[********]", HashedPassword.FromPlaintext("Secret123", Hasher).ToString());
}
