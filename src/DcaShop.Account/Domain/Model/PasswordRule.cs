namespace DcaShop.Account.Domain.Model;

/// <summary>
/// The part of the password policy a plaintext password breaks — what <see cref="PasswordTooWeakException"/>
/// names, so that whoever shows the refusal can word it.
/// </summary>
public enum PasswordRule
{
    /// <summary>Shorter than <see cref="HashedPassword.MinLength"/> characters.</summary>
    TooShort,

    /// <summary>Longer than <see cref="HashedPassword.MaxByteLength"/> bytes, UTF-8 encoded.</summary>
    TooLong,

    /// <summary>No uppercase letter.</summary>
    NoUppercaseLetter,

    /// <summary>No lowercase letter.</summary>
    NoLowercaseLetter,

    /// <summary>No digit.</summary>
    NoDigit,
}