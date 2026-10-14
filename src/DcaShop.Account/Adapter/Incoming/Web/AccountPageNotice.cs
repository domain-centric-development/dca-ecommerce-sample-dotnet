using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// The notices a redirect leaves for an account page. A notice travels in TempData — a cookie sealed with the key ring
/// every instance shares — as its fixed code, never the sentence and never in a server session, so any instance that
/// answers the next request shows it. It is addressed to one page: only that page reads it, and reading removes it.
/// </summary>
internal enum AccountPageNotice
{
    /// <summary>The profile was saved.</summary>
    ProfileUpdated = 1,

    /// <summary>The password was changed.</summary>
    PasswordChanged = 2,
}

/// <summary>Each notice's code, the sentence its page shows, and where a page finds the notice left for it.</summary>
internal static class AccountPageNotices
{
    /// <summary>The TempData key of the notice addressed to the profile page.</summary>
    public const string ProfilePage = "notice.account.profile";

    /// <summary>The TempData key of the notice addressed to the change-password page.</summary>
    public const string ChangePasswordPage = "notice.account.change-password";

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

    /// <summary>The notice a code names; none for a code the shop does not know.</summary>
    public static AccountPageNotice? FromCode(string? code) =>
        Enum.GetValues<AccountPageNotice>().Cast<AccountPageNotice?>().FirstOrDefault(notice => notice?.Code() == code);

    /// <summary>Leaves the notice's code for the page the redirect leads to.</summary>
    public static void Leave(this ITempDataDictionary tempData, string page, AccountPageNotice notice) =>
        tempData[page] = notice.Code();

    /// <summary>The notice left for this page, if any — reading it removes it.</summary>
    public static AccountPageNotice? TakeAccountPageNotice(this ITempDataDictionary tempData, string page) =>
        FromCode(tempData[page] as string);
}
