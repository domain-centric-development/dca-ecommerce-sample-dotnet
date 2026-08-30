using System.Globalization;
using DcaShop.Account.Application.GetAccountOverview;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>What the account landing page shows.</summary>
public sealed record MyAccountPageViewModel(
    string Email,
    string LastLoginDisplay,
    IReadOnlyList<AccountNavigation.NavItem> NavItems)
{
    /// <summary>Shown instead of a timestamp for an account that has never been logged into.</summary>
    public const string NeverLoggedIn = "Never";

    public static MyAccountPageViewModel From(GetAccountOverviewResult.AccountOverview overview)
    {
        ArgumentNullException.ThrowIfNull(overview);
        return new MyAccountPageViewModel(
            overview.Email,
            FormatLastLogin(overview.LastLoginAt),
            AccountNavigation.ItemsWithActive(AccountNavigation.Overview));
    }

    private static string FormatLastLogin(DateTimeOffset? lastLoginAt) =>
        lastLoginAt is null
            ? NeverLoggedIn
            : lastLoginAt.Value.UtcDateTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
}
