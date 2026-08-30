using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>The identity a request runs under, as read from the cookies or the Authorization header.</summary>
public sealed record JwtIdentity(
    UserId UserId,
    IIdentityProvider.IdentityType Type,
    string? Email,
    IReadOnlySet<string> Roles) : IIdentityProvider.IIdentity
{
    /// <summary>
    /// A visitor with an identity but no session. The <see cref="UserId"/> is deliberately the one the browser
    /// already carried: expiry ends the session, not the identity (ADR-029).
    /// </summary>
    public static JwtIdentity Anonymous(UserId userId) =>
        new(userId, IIdentityProvider.IdentityType.Anonymous, null, new HashSet<string>());

    public static JwtIdentity Registered(UserId userId, string email, IReadOnlySet<string> roles) =>
        new(userId, IIdentityProvider.IdentityType.Registered, email, roles);
}
