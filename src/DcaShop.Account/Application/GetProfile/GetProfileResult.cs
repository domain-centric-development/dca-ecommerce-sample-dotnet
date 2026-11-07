using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Account.Application.GetProfile;

/// <summary>The profile, or no account the session may act on.</summary>
[GenerateOneOf]
public sealed partial class GetProfileResult : OneOfBase<GetProfileResult.Found, GetProfileResult.AccountNotAccessible>
{
    /// <summary>The account's profile.</summary>
    public sealed record Found(ProfileView Profile);

    /// <summary>The identity has no account the session may act on — closed, suspended, or none.</summary>
    public sealed record AccountNotAccessible : IRefusal;

    /// <summary>What the profile page shows. The name is read-only: it is fixed at registration.</summary>
    public sealed record ProfileView(string Email, string FirstName, string LastName, DateOnly DateOfBirth);
}