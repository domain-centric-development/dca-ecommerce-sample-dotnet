using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Specification;

/// <summary>
/// A date of birth is usable when it is known and does not lie in the future.
/// </summary>
/// <remarks>
/// A first-class rule because two components evaluate it: <see cref="Model.Owner"/> refuses to exist without a
/// usable date, and the change-profile use case rejects a submitted one before touching the aggregate, so a
/// refused submission cannot leave a half-applied change behind. It deliberately says nothing about plausible
/// ages — an arbitrary upper bound would refuse real people.
/// </remarks>
public sealed class UsableDateOfBirth : ISpecification<DateOnly?>
{
    /// <summary>The rule itself; it carries no state.</summary>
    public static readonly UsableDateOfBirth Rule = new();

    private UsableDateOfBirth()
    {
    }

    public bool IsSatisfiedBy(DateOnly? candidate) =>
        candidate is not null && candidate.Value <= DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Throws with the user-facing message naming which half of the rule failed.</summary>
    public void RequireSatisfiedBy(DateOnly? candidate)
    {
        if (candidate is null)
        {
            throw new ArgumentException("Date of birth is required");
        }

        if (candidate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentException("Date of birth cannot be in the future");
        }
    }
}
