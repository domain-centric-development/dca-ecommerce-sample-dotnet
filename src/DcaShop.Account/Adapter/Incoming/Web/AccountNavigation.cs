namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>The side navigation shared by every account page.</summary>
public static class AccountNavigation
{
    public const string Overview = "overview";
    public const string Profile = "profile";
    public const string ChangePassword = "change-password";
    public const string Orders = "orders";

    public static IReadOnlyList<NavItem> ItemsWithActive(string activeKey) =>
    [
        new(Overview, "Overview", AccountRoutes.Account, activeKey),
        new(Profile, "My Profile", AccountRoutes.Profile, activeKey),
        new(ChangePassword, "Change Password", AccountRoutes.ChangePassword, activeKey),

        // Orders has no page yet; it shows as a disabled item rather than a dead link.
        new(Orders, "My Orders", null, activeKey),
    ];

    /// <summary>One entry of the account navigation. An item without a target is shown but not clickable.</summary>
    public sealed record NavItem(string Key, string Label, string? Href, bool Active)
    {
        internal NavItem(string key, string label, string? href, string activeKey)
            : this(key, label, href, key == activeKey)
        {
        }

        public bool Navigable => Href is not null;
    }
}
