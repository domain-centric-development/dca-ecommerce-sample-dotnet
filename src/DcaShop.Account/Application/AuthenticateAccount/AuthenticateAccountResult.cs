using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Application.AuthenticateAccount;

/// <summary>
/// The outcome of a login attempt. A failure names a message meant for the user, and for every reason a
/// credential could be wrong that message is the same one — naming which half failed tells an attacker which
/// addresses exist.
/// </summary>
public sealed record AuthenticateAccountResult(
    bool Success,
    string? UserId,
    string? Email,
    IReadOnlySet<Role>? Roles,
    string? ErrorMessage)
{
    public static AuthenticateAccountResult Succeeded(string userId, string email, IReadOnlySet<Role> roles) =>
        new(true, userId, email, roles, null);

    public static AuthenticateAccountResult Failed(string errorMessage) =>
        new(false, null, null, null, errorMessage);
}