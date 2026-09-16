using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Account.Application.Shared;

/// <summary>
/// Establishes and ends the authenticated session of the current browser. Only the Account context modifies a
/// session; other contexts read the resulting identity through
/// <see cref="SharedKernel.Application.Shared.IIdentityProvider"/>.
/// </summary>
/// <remarks>
/// The implementation writes cookies on the current response and is therefore request-scoped. Whether cookie
/// mechanics belong behind an output port at all is an open question for both samples (root <c>TODO.md</c>);
/// the shape here is the Java sample's, deliberately unchanged.
/// </remarks>
public interface IIdentitySession : IOutputPort
{
    /// <summary>
    /// Starts an authenticated session for the given token and aligns the visitor identity with the account it
    /// names, so a later session expiry drops the browser onto the account's cart rather than a superseded one.
    /// </summary>
    void SetRegisteredIdentity(string token);

    /// <summary>
    /// Ends the session and rotates the visitor identity: the next person on a shared device must not inherit
    /// this cart, while the account's own cart returns at the next login.
    /// </summary>
    void LogOut();
}
