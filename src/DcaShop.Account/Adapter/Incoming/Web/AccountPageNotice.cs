using System.Net;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// The notices a redirect carries to an account page, as a fixed code in its address (<c>?notice=&lt;code&gt;</c>) —
/// never the sentence and never in a server session, so any instance that answers the next request shows it.
/// </summary>
internal enum AccountPageNotice
{
    /// <summary>The profile was saved.</summary>
    ProfileUpdated = 1,

    /// <summary>The password was changed.</summary>
    PasswordChanged = 2,
}

/// <summary>Each notice's code in the address and the sentence its page shows.</summary>
internal static class AccountPageNotices
{
    /// <summary>The query parameter that carries a notice's code.</summary>
    public const string Parameter = "notice";

    public static string Code(this AccountPageNotice notice) => notice switch
    {
        AccountPageNotice.ProfileUpdated => "profile-updated",
        AccountPageNotice.PasswordChanged => "password-changed",
        _ => throw new ArgumentOutOfRangeException(nameof(notice)),
    };

    public static string Sentence(this AccountPageNotice notice) => notice switch
    {
        AccountPageNotice.ProfileUpdated => "Your profile has been updated.",
        AccountPageNotice.PasswordChanged => "Your password has been changed.",
        _ => throw new ArgumentOutOfRangeException(nameof(notice)),
    };

    /// <summary>The notice an address names by its code; none for a code the shop does not know.</summary>
    public static AccountPageNotice? FromCode(string? code) =>
        Enum.GetValues<AccountPageNotice>().Cast<AccountPageNotice?>().FirstOrDefault(notice => notice?.Code() == code);

    /// <summary>The page's path carrying the notice.</summary>
    public static string PathWith(string path, AccountPageNotice notice) =>
        $"{path}?{Parameter}={WebUtility.UrlEncode(notice.Code())}";
}