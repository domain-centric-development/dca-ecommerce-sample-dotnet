using DcaShop.Account.Application.GetProfile;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// What the profile page shows. On a rejection the submitted values are kept so the visitor can correct them,
/// while the name always comes from the stored profile — it cannot be submitted at all.
/// </summary>
public sealed record ProfilePageViewModel(
    string FullName,
    string Email,
    string DateOfBirth,
    IReadOnlyList<AccountNavigation.NavItem> NavItems,
    string? SuccessMessage,
    string? ErrorMessage)
{
    public static ProfilePageViewModel Of(GetProfileResult.ProfileView stored, string? successMessage)
    {
        ArgumentNullException.ThrowIfNull(stored);
        return new ProfilePageViewModel(
            FullName: $"{stored.FirstName} {stored.LastName}",
            Email: stored.Email,
            DateOfBirth: stored.DateOfBirth.ToString("yyyy-MM-dd"),
            NavItems: AccountNavItems(),
            SuccessMessage: successMessage,
            ErrorMessage: null);
    }

    public static ProfilePageViewModel WithError(
        GetProfileResult.ProfileView stored, string submittedEmail, string submittedDateOfBirth, string message)
    {
        ArgumentNullException.ThrowIfNull(stored);
        return new ProfilePageViewModel(
            FullName: $"{stored.FirstName} {stored.LastName}",
            Email: submittedEmail,
            DateOfBirth: submittedDateOfBirth,
            NavItems: AccountNavItems(),
            SuccessMessage: null,
            ErrorMessage: message);
    }

    private static IReadOnlyList<AccountNavigation.NavItem> AccountNavItems() =>
        AccountNavigation.ItemsWithActive(AccountNavigation.Profile);
}
