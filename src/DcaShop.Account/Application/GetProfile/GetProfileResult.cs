namespace DcaShop.Account.Application.GetProfile;

/// <summary>The profile, or nothing when the identity has no account the session may act on.</summary>
public sealed record GetProfileResult(GetProfileResult.ProfileView? Profile)
{
    public bool Found => Profile is not null;

    public static GetProfileResult NotFound() => new((ProfileView?)null);

    /// <summary>What the profile page shows. The name is read-only: it is fixed at registration.</summary>
    public sealed record ProfileView(string Email, string FirstName, string LastName, DateOnly DateOfBirth);
}
