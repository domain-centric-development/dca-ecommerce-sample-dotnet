using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Model;

/// <summary>Raised when a plaintext password does not meet the account's strength rules.</summary>
/// <remarks>
/// A rule of the model, not an argument contract: the caller passed a perfectly well-formed string, and the
/// model refuses it for what it is made of. Its own type is what lets a use case report the refusal to the
/// person choosing the password, while a malformed call from the hashing adapter keeps travelling as the defect
/// it is. It names the broken <see cref="PasswordRule"/>; the message is for the log, and whoever shows the
/// refusal words it from the rule.
/// </remarks>
public sealed class PasswordTooWeakException : DomainException
{
    public PasswordTooWeakException(PasswordRule rule)
        : base(Describe(rule))
    {
        Rule = rule;
    }

    /// <summary>The part of the policy the password breaks.</summary>
    public PasswordRule Rule { get; }

    private static string Describe(PasswordRule rule) => rule switch
    {
        PasswordRule.TooShort => $"Password must be at least {HashedPassword.MinLength} characters long",
        PasswordRule.TooLong => $"Password must not be longer than {HashedPassword.MaxByteLength} bytes (UTF-8 encoded)",
        PasswordRule.NoUppercaseLetter => "Password must contain at least one uppercase letter",
        PasswordRule.NoLowercaseLetter => "Password must contain at least one lowercase letter",
        PasswordRule.NoDigit => "Password must contain at least one digit",
        _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, null),
    };
}