using System.Net;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>The account paths, in one place because the pages link to each other.</summary>
internal static class AccountRoutes
{
    public const string Account = "/account";
    public const string Profile = "/account/profile";
    public const string ChangePassword = "/account/change-password";
    public const string Login = "/login";

    /// <summary>
    /// Sends an unauthenticated visitor to the login form and back to where they wanted to go.
    /// </summary>
    public static string ToLoginWithReturnUrl(string path) =>
        $"{Login}?returnUrl={WebUtility.UrlEncode(path)}";
}
