using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Api;

/// <summary>
/// Who the current request belongs to, and what they are allowed to be. Published by <see cref="IIdentityService"/>.
/// </summary>
/// <remarks>
/// The <see cref="UserId"/> is the cross-context identity: it survives session expiry and changes only on explicit
/// logout, so it is the same before and after a login — authentication adds a session, it does not replace who the
/// browser is.
/// </remarks>
/// <param name="UserId">The caller's identity, always present.</param>
/// <param name="Type">Whether the caller has authenticated.</param>
/// <param name="Email">The account's email, present for a registered caller only.</param>
/// <param name="Roles">The account's roles, empty for an anonymous caller.</param>
public sealed record Identity(UserId UserId, IdentityType Type, string? Email, IReadOnlySet<string> Roles)
{
    /// <summary>The role every registered account holds.</summary>
    public const string RoleCustomer = "CUSTOMER";

    /// <summary>
    /// The operator role. It guards what a shopper must never reach — listing every customer's cart, creating a
    /// product — and no registration path hands it out: an account only gets it by being given it.
    /// </summary>
    public const string RoleStaff = "STAFF";

    /// <summary>An identity without a session, keeping the <see cref="UserId"/> the browser already carries.</summary>
    public static Identity Anonymous(UserId userId) => new(userId, IdentityType.Anonymous, null, new HashSet<string>());

    /// <summary>The identity of an authenticated account.</summary>
    public static Identity Registered(UserId userId, string email, IReadOnlySet<string> roles) =>
        new(userId, IdentityType.Registered, email, roles);

    public bool IsAnonymous => Type == IdentityType.Anonymous;

    public bool IsRegistered => Type == IdentityType.Registered;

    public bool HasRole(string role) => Roles.Contains(role);
}
