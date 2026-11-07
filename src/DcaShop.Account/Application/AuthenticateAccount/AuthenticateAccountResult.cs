using DcaShop.Account.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Account.Application.AuthenticateAccount;

/// <summary>
/// What a login attempt came to: authenticated, the credentials were not accepted, or the account may not log in.
/// The result says what happened, never in words: the adapter that shows it words the refusal.
/// </summary>
[GenerateOneOf]
public sealed partial class AuthenticateAccountResult
    : OneOfBase<
        AuthenticateAccountResult.Authenticated,
        AuthenticateAccountResult.CredentialsRefused,
        AuthenticateAccountResult.AccountNotAccessible>
{
    /// <summary>The credentials belong to an account that may log in; the login was recorded.</summary>
    public sealed record Authenticated(string UserId, string Email, IReadOnlySet<Role> Roles);

    /// <summary>
    /// The address and password do not name an account together. One case for every reason a credential could be
    /// wrong — an unknown address, a wrong password, a malformed address: naming which half failed tells an attacker
    /// which addresses exist.
    /// </summary>
    public sealed record CredentialsRefused : IRefusal;

    /// <summary>The credentials are right, but the account's status does not allow a login.</summary>
    /// <param name="Status">The status that refuses the login.</param>
    public sealed record AccountNotAccessible(AccountStatus Status) : IRefusal;
}