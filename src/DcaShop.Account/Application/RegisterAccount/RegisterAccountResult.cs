namespace DcaShop.Account.Application.RegisterAccount;

/// <summary>The registered account, as the adapter needs it to mint a session token.</summary>
public sealed record RegisterAccountResult(
    string AccountId,
    string UserId,
    string Email,
    IReadOnlySet<string> Roles);
