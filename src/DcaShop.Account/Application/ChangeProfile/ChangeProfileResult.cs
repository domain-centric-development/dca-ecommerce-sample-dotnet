using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Account.Application.ChangeProfile;

/// <summary>
/// What a profile change came to. The result says what happened and which value was refused, never in words: the
/// page that shows it words the refusal from the case. Only a stored change carries the profile.
/// </summary>
[GenerateOneOf]
public sealed partial class ChangeProfileResult
    : OneOfBase<
        ChangeProfileResult.Changed,
        ChangeProfileResult.AccountNotAccessible,
        ChangeProfileResult.EmailAlreadyInUse,
        ChangeProfileResult.EmailInvalid,
        ChangeProfileResult.DateOfBirthInvalid>
{
    /// <summary>The change was stored.</summary>
    /// <param name="Profile">The stored values after the change.</param>
    public sealed record Changed(ProfileView Profile);

    /// <summary>No account the session may act on — closed, suspended, or gone.</summary>
    public sealed record AccountNotAccessible : IRefusal;

    /// <summary>Another account already uses the submitted email address.</summary>
    public sealed record EmailAlreadyInUse : IRefusal;

    /// <summary>The submitted email address is not a valid address.</summary>
    public sealed record EmailInvalid : IRefusal;

    /// <summary>The submitted date of birth is missing or lies in the future.</summary>
    public sealed record DateOfBirthInvalid : IRefusal;

    /// <summary>The stored values after the change.</summary>
    public sealed record ProfileView(string Email, DateOnly DateOfBirth);
}