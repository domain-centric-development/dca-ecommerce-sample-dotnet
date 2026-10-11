using System.Text;

using DcaShop.Account.Domain.Gateway;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Model;

/// <summary>
/// A securely hashed password. It never holds plaintext and delegates the cryptographic work to the
/// <see cref="IPasswordHasher"/> domain gateway.
/// </summary>
public sealed record HashedPassword : IValue
{
    /// <summary>Minimum length of an acceptable plaintext password.</summary>
    public const int MinLength = 8;

    /// <summary>
    /// Maximum length of an acceptable plaintext password, in <b>bytes</b> rather than characters, because that
    /// is the bound hashing algorithms impose — BCrypt rejects input beyond 72 bytes. Keeping the policy at that
    /// value means an over-long password is refused as a rule the user broke, and can never
    /// reach the hasher and fail there as a technical fault.
    /// </summary>
    public const int MaxByteLength = 72;

    private HashedPassword(string hash) => Hash = hash;

    public string Hash { get; }

    /// <summary>Validates the plaintext against the password policy and hashes it.</summary>
    public static HashedPassword FromPlaintext(string plaintext, IPasswordHasher hasher)
    {
        ValidatePasswordStrength(plaintext);
        return new HashedPassword(hasher.Hash(plaintext));
    }

    /// <summary>Wraps an already computed hash, e.g. when reconstituting from storage.</summary>
    public static HashedPassword FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("Password hash cannot be null or blank", nameof(hash));
        }

        return new HashedPassword(hash);
    }

    public bool Matches(string plaintext, IPasswordHasher hasher) => hasher.Matches(plaintext, Hash);

    /// <summary>
    /// The password policy: at least <see cref="MinLength"/> characters, at most <see cref="MaxByteLength"/>
    /// bytes UTF-8 encoded, and at least one uppercase letter, one lowercase letter and one digit.
    /// </summary>
    /// <remarks>
    /// The refusal names the broken <see cref="PasswordRule"/>; its message is for the log. A page or an API words
    /// the refusal from the rule.
    /// </remarks>
    public static void ValidatePasswordStrength(string plaintext)
    {
        if (plaintext is null || plaintext.Length < MinLength)
        {
            throw new PasswordTooWeakException(PasswordRule.TooShort);
        }

        if (Encoding.UTF8.GetByteCount(plaintext) > MaxByteLength)
        {
            throw new PasswordTooWeakException(PasswordRule.TooLong);
        }

        if (!plaintext.Any(char.IsUpper))
        {
            throw new PasswordTooWeakException(PasswordRule.NoUppercaseLetter);
        }

        if (!plaintext.Any(char.IsLower))
        {
            throw new PasswordTooWeakException(PasswordRule.NoLowercaseLetter);
        }

        if (!plaintext.Any(char.IsDigit))
        {
            throw new PasswordTooWeakException(PasswordRule.NoDigit);
        }
    }

    public override string ToString() => "HashedPassword[********]";
}