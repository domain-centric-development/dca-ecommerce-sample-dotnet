using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Account.Api;

/// <summary>
/// Open Host Service of the Account context: the identity of the current caller.
/// </summary>
/// <remarks>
/// Account is the only context that establishes who is making a request — from the identity and session cookies or
/// the Bearer header. Every other context keys its data on that <c>UserId</c>, so this service publishes the resolved
/// identity for their incoming adapters. An adapter reads it once per request and passes the customer into its
/// command or query; the use cases themselves never depend on it. Implemented by the Account context's security
/// adapter, which reads what the authentication handler resolved for the current request.
/// </remarks>
[OpenHostService("Account", Description = "The identity of the current caller for the incoming adapters of other bounded contexts")]
public interface IIdentityService
{
    /// <summary>
    /// The identity of the current request. Never <see langword="null"/>: a visitor who has not authenticated is
    /// anonymous, not absent.
    /// </summary>
    /// <exception cref="InvalidOperationException">outside a request the Account context authenticated.</exception>
    Identity CurrentIdentity();
}
