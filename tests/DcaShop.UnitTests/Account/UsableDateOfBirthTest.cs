using DcaShop.Account.Domain.Specification;

namespace DcaShop.UnitTests.Account;

public sealed class UsableDateOfBirthTest
{
    [Fact]
    public void APastDateIsUsable() =>
        Assert.True(UsableDateOfBirth.Rule.IsSatisfiedBy(new DateOnly(1990, 5, 17)));

    [Fact]
    public void TodayIsUsable() =>
        Assert.True(UsableDateOfBirth.Rule.IsSatisfiedBy(DateOnly.FromDateTime(DateTime.UtcNow)));

    [Fact]
    public void AFutureDateIsNot() =>
        Assert.False(UsableDateOfBirth.Rule.IsSatisfiedBy(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)));

    [Fact]
    public void AMissingDateIsNot() => Assert.False(UsableDateOfBirth.Rule.IsSatisfiedBy(null));

    [Fact]
    public void TheFailureNamesWhichHalfOfTheRuleBroke()
    {
        Assert.Equal(
            "Date of birth is required",
            Assert.Throws<ArgumentException>(() => UsableDateOfBirth.Rule.RequireSatisfiedBy(null)).Message);

        Assert.Equal(
            "Date of birth cannot be in the future",
            Assert.Throws<ArgumentException>(
                () => UsableDateOfBirth.Rule.RequireSatisfiedBy(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)))
                .Message);
    }

    [Fact]
    public void ANoAgeLimitIsDeliberate() =>
        // An arbitrary upper bound would refuse real people.
        Assert.True(UsableDateOfBirth.Rule.IsSatisfiedBy(new DateOnly(1900, 1, 1)));
}
