namespace DcaShop.Account.Adapter.Incoming.Api;

public sealed record RegisterResponse(bool Success, string? Token, string? Email, string? ErrorMessage)
{
    public static RegisterResponse Succeeded(string token, string email) => new(true, token, email, null);

    public static RegisterResponse Failed(string? errorMessage) => new(false, null, null, errorMessage);
}
