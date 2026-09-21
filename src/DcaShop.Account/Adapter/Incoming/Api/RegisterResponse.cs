namespace DcaShop.Account.Adapter.Incoming.Api;

/// <summary>The register endpoint's body — the success case only.</summary>
/// <remarks>
/// A refused registration travels as a problem document built by <see cref="AccountApiExceptionHandler"/>, so
/// this record carries no error field: a body that can describe both outcomes invites a client to read the
/// status from the body instead of from the response.
/// </remarks>
public sealed record RegisterResponse(bool Success, string Token, string Email)
{
    public static RegisterResponse Succeeded(string token, string email) => new(true, token, email);
}
