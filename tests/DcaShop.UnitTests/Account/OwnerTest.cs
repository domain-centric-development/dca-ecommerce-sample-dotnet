using DcaShop.Account.Domain.Model;

namespace DcaShop.UnitTests.Account;

public sealed class OwnerTest
{
    private static readonly DateOnly Born = new(1990, 5, 17);

    [Fact]
    public void NamesAreTrimmedButNotOtherwiseNormalized()
    {
        var owner = Owner.Of("  van der Berg ", " O'Neill  ", Born);

        Assert.Equal("van der Berg", owner.FirstName);
        Assert.Equal("O'Neill", owner.LastName);
    }

    [Theory]
    [InlineData("", "Doe")]
    [InlineData("   ", "Doe")]
    [InlineData("Jane", "")]
    public void ANameIsRequired(string firstName, string lastName) =>
        Assert.Throws<ArgumentException>(() => Owner.Of(firstName, lastName, Born));

    [Fact]
    public void ANameLongerThanAHundredCharactersIsRefused() =>
        Assert.Throws<ArgumentException>(() => Owner.Of(new string('a', 101), "Doe", Born));

    [Fact]
    public void ADateOfBirthIsRequired() =>
        Assert.Throws<ArgumentException>(() => Owner.Of("Jane", "Doe", null));

    [Fact]
    public void ADateOfBirthInTheFutureIsRefused() =>
        Assert.Throws<ArgumentException>(
            () => Owner.Of("Jane", "Doe", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)));

    [Fact]
    public void CorrectingTheDateCarriesBothNamesOver()
    {
        var corrected = Owner.Of("Jane", "Doe", Born).WithDateOfBirth(new DateOnly(1991, 6, 18));

        Assert.Equal("Jane", corrected.FirstName);
        Assert.Equal("Doe", corrected.LastName);
        Assert.Equal(new DateOnly(1991, 6, 18), corrected.DateOfBirth);
    }
}
