using DcaShop.Account.Domain.Specification;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Model;

/// <summary>
/// The natural person an account belongs to: first name, last name and date of birth. The name identifies who
/// the account belongs to and is therefore fixed for the lifetime of the account — it is captured once at
/// registration and no operation replaces it. Only the date of birth may be corrected, via
/// <see cref="WithDateOfBirth"/>, which carries both names over unchanged.
/// </summary>
/// <remarks>
/// Names are trimmed but never otherwise normalized — capitalization, particles and spelling of a person's own
/// name are not the account's to correct — and are limited to 100 characters. Immutability of the name is a
/// property of the type rather than a rule callers have to remember: there is no setter, and the one derivation
/// copies the names.
/// </remarks>
public sealed record Owner : IValue
{
    private const int MaxNameLength = 100;

    private Owner(string firstName, string lastName, DateOnly dateOfBirth)
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
    }

    public string FirstName { get; }

    public string LastName { get; }

    public DateOnly DateOfBirth { get; }

    public static Owner Of(string firstName, string lastName, DateOnly? dateOfBirth)
    {
        UsableDateOfBirth.Rule.RequireSatisfiedBy(dateOfBirth);
        return new Owner(RequireName(firstName, "First name"), RequireName(lastName, "Last name"), dateOfBirth!.Value);
    }

    /// <summary>Derives an owner with a corrected date of birth, carrying both names over unchanged.</summary>
    public Owner WithDateOfBirth(DateOnly newDateOfBirth)
    {
        UsableDateOfBirth.Rule.RequireSatisfiedBy(newDateOfBirth);
        return new Owner(FirstName, LastName, newDateOfBirth);
    }

    public string FullName() => $"{FirstName} {LastName}";

    private static string RequireName(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{label} is required");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException($"{label} is too long");
        }

        return trimmed;
    }
}
