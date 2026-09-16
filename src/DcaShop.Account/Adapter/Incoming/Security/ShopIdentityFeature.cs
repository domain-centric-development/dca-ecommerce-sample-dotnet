using DcaShop.Account.Api;

namespace DcaShop.Account.Adapter.Incoming.Security;

/// <summary>
/// The identity <see cref="ShopIdentityAuthenticationHandler"/> resolved for the current request, in the shape the
/// Account Api publishes, as an <c>HttpContext</c> feature.
/// </summary>
/// <remarks>
/// <c>HttpContext.User</c> is the framework's view and is not stable across the request: an <c>[Authorize]</c> that
/// names another scheme — the backoffice's cookie — replaces it with that scheme's principal, and an anonymous
/// visitor has no authenticated principal at all. The published identity must answer on every page regardless (the header's mini
/// basket is keyed on the visitor), so the handler records the identity here, the way the framework records its own
/// authentication result as a feature.
/// </remarks>
public interface IShopIdentityFeature
{
    Identity Identity { get; }
}

/// <inheritdoc cref="IShopIdentityFeature"/>
public sealed record ShopIdentityFeature(Identity Identity) : IShopIdentityFeature;
