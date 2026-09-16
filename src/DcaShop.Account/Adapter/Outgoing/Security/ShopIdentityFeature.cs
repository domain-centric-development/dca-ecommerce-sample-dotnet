using DcaShop.SharedKernel.Application.Shared;

namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>
/// The identity <see cref="ShopIdentityAuthenticationHandler"/> resolved for the current request, in the port's
/// shape, as an <c>HttpContext</c> feature.
/// </summary>
/// <remarks>
/// <c>HttpContext.User</c> is the framework's view and is not stable across the request: an <c>[Authorize]</c> that
/// names another scheme — the backoffice's cookie — replaces it with that scheme's principal, and an anonymous
/// visitor has no authenticated principal at all. The port must answer on every page regardless (the header's mini
/// basket is keyed on the visitor), so the handler records the identity here, the way the framework records its own
/// authentication result as a feature.
/// </remarks>
public interface IShopIdentityFeature
{
    IIdentityProvider.IIdentity Identity { get; }
}

/// <inheritdoc cref="IShopIdentityFeature"/>
public sealed record ShopIdentityFeature(IIdentityProvider.IIdentity Identity) : IShopIdentityFeature;
