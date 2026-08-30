namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// What the change-password page shows. It never carries a submitted password back into the form.
/// </summary>
public sealed record ChangePasswordPageViewModel(
    IReadOnlyList<AccountNavigation.NavItem> NavItems,
    string? SuccessMessage,
    string? ErrorMessage)
{
    public static ChangePasswordPageViewModel Blank(string? successMessage) =>
        new(AccountNavItems(), successMessage, null);

    public static ChangePasswordPageViewModel WithError(string message) => new(AccountNavItems(), null, message);

    private static IReadOnlyList<AccountNavigation.NavItem> AccountNavItems() =>
        AccountNavigation.ItemsWithActive(AccountNavigation.ChangePassword);
}
