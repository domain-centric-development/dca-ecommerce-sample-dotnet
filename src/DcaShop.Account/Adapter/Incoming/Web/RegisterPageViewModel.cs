namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>
/// What the registration form shows. A rejected submission is re-rendered with everything the visitor typed
/// except the passwords, so they only have to fix what was wrong.
/// </summary>
public sealed record RegisterPageViewModel(
    string? Email,
    string? FirstName,
    string? LastName,
    string? DateOfBirth,
    string? ReturnUrl,
    string? ErrorMessage)
{
    public static RegisterPageViewModel Blank(string? returnUrl) => new(null, null, null, null, returnUrl, null);
}
