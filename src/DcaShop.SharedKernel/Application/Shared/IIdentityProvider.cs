using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.SharedKernel.Application.Shared;

/// <summary>
/// Reads the identity of the visitor the current request belongs to. Every context keys its data on the
/// <see cref="UserId"/> this returns — the cart, the checkout session — which is why the port lives in the
/// shared kernel and not in the Account context, even though only Account can establish an authenticated one.
/// </summary>
/// <remarks>
/// Its only implementation lives in the Account context, which is a dependency the context map does not show;
/// recorded as an open item in the root <c>TODO.md</c>, together with the question whether a request-context
/// reader is an output port at all.
/// </remarks>
public interface IIdentityProvider : IOutputPort
{
    /// <summary>
    /// The identity of the current request. Never <see langword="null"/>: a visitor who has not authenticated is
    /// anonymous, not absent.
    /// </summary>
    IIdentity GetCurrentIdentity();

    /// <summary>
    /// Who the current request belongs to, and what they are allowed to be. Nested in the port because it is
    /// part of its contract rather than a port of its own.
    /// </summary>
    public interface IIdentity
    {
        /// <summary>The cross-context identity. It survives session expiry and changes only on explicit logout.</summary>
        UserId UserId { get; }

        IdentityType Type { get; }

        /// <summary>The email of the authenticated account, absent for an anonymous visitor.</summary>
        string? Email { get; }

        IReadOnlySet<Role> Roles { get; }

        /// <summary>
        /// The anonymous identity the login that started the current session replaced. Absent for an anonymous
        /// visitor and wherever the session does not record it.
        /// </summary>
        UserId? GuestBeforeLogin { get; }

        bool IsAnonymous => Type == IdentityType.Anonymous;

        bool IsRegistered => Type == IdentityType.Registered;

        bool HasRole(Role role) => Roles.Contains(role);
    }

    /// <summary>Whether the visitor has authenticated in the current session.</summary>
    public enum IdentityType
    {
        /// <summary>A visitor with an identity but no session. The normal state of a shopper who has not logged in.</summary>
        Anonymous,

        /// <summary>A visitor whose session names an existing account.</summary>
        Registered,
    }
}