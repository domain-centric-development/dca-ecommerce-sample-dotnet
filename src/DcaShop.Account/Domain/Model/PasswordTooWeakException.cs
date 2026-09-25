using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Account.Domain.Model;

/// <summary>Raised when a plaintext password does not meet the account's strength rules.</summary>
/// <remarks>
/// A rule of the model, not an argument contract: the caller passed a perfectly well-formed string, and the
/// model refuses it for what it is made of. Its own type is what lets a use case show the reason to the person
/// choosing the password, while a malformed call from the hashing adapter keeps travelling as the defect it
/// is.
/// </remarks>
public sealed class PasswordTooWeakException : DomainException
{
    public PasswordTooWeakException(string reason)
        : base(reason)
    {
    }
}