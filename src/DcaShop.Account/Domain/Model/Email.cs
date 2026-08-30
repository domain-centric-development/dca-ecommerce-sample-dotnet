using System.Text.RegularExpressions;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Model;

/// <summary>
/// Validated, normalized (lower-cased) email address. It is also the unique login credential, which is why a
/// change of address re-issues the session token.
/// </summary>
public sealed partial record Email : IValue
{
    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email cannot be null or blank");
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!EmailPattern().IsMatch(normalized))
        {
            throw new ArgumentException($"Invalid email format: {normalized}");
        }

        return new Email(normalized);
    }

    public string LocalPart() => Value[..Value.IndexOf('@')];

    public string Domain() => Value[(Value.IndexOf('@') + 1)..];

    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailPattern();
}
