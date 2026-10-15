namespace DcaShop.Account.Adapter.Incoming.Api;

/// <summary>The login endpoint's body — the success case only.</summary>
/// <remarks>
/// A refused login travels as a problem document built by <see cref="AuthResource"/>, so this record carries no
/// error field, as <see cref="RegisterResponse"/> carries none.
/// </remarks>
public sealed record LoginResponse(bool Success, string Token, string Email)
{
    public static LoginResponse Succeeded(string token, string email) => new(true, token, email);
}