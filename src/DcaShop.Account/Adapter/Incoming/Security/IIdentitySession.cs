namespace DcaShop.Account.Adapter.Incoming.Security;

/// <summary>
/// Establishes and ends the authenticated session of the current browser. Only the Account context modifies a
/// session; other contexts read the resulting identity through
/// <see cref="SharedKernel.Application.Shared.IIdentityProvider"/>.
/// </summary>
/// <remarks>
/// Adapter-internal mechanics, not a port: setting and clearing cookies belongs to the incoming adapter that owns
/// the HTTP protocol, and no use case depends on it. The interface therefore lives with its callers in the adapter
/// layer and carries no <c>IOutputPort</c> marker. The cookie-writing implementation is <c>JwtIdentitySession</c>
/// in the security adapter; it is request-scoped because it needs the current response.
/// </remarks>
public interface IIdentitySession
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