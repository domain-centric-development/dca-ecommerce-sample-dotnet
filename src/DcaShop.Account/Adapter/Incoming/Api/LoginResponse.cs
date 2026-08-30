namespace DcaShop.Account.Adapter.Incoming.Api;

/// <summary>The token a successful login hands out, or the one message every failed one gives.</summary>
public sealed record LoginResponse(bool Success, string? Token, string? Email, string? ErrorMessage)
{
    public static LoginResponse Succeeded(string token, string email) => new(true, token, email, null);

    public static LoginResponse Failed(string? errorMessage) => new(false, null, null, errorMessage);
}
