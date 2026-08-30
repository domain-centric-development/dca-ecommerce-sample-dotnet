namespace DcaShop.Account.Adapter.Incoming.Web;

/// <summary>What the login page shows: a rejected attempt keeps the address, never the password.</summary>
public sealed record LoginPageViewModel(
    string? ReturnUrl,
    string? Email,
    string? ErrorMessage,
    bool LoggedOut);
