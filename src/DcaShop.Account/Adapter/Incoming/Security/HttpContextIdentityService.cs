using DcaShop.Account.Api;
using Microsoft.AspNetCore.Http;

namespace DcaShop.Account.Adapter.Incoming.Security;

/// <summary>
/// Implements the Account context's <see cref="IIdentityService"/>: reads the identity
/// <see cref="ShopIdentityAuthenticationHandler"/> resolved for the current request from its
/// <see cref="IShopIdentityFeature"/>.
/// </summary>
public sealed class HttpContextIdentityService : IIdentityService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextIdentityService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public Identity CurrentIdentity()
    {
        var context = _httpContextAccessor.HttpContext
                      ?? throw new InvalidOperationException(
                          "No HTTP context: an identity exists only inside a request");

        return context.Features.Get<IShopIdentityFeature>()?.Identity
               ?? throw new InvalidOperationException(
                   "No identity on the request. This usually means it did not pass through the shop's "
                   + "authentication scheme.");
    }
}
