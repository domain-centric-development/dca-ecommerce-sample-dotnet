using DcaShop.SharedKernel.Application.Shared;
using Microsoft.AspNetCore.Http;

namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>
/// Reads the identity <see cref="JwtAuthenticationMiddleware"/> resolved for the current request.
/// </summary>
public sealed class HttpContextIdentityProvider : IIdentityProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextIdentityProvider(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public IIdentityProvider.IIdentity GetCurrentIdentity()
    {
        var context = _httpContextAccessor.HttpContext
                      ?? throw new InvalidOperationException(
                          "No HTTP context: an identity exists only inside a request");

        return context.Items[JwtAuthenticationMiddleware.IdentityItemKey] as IIdentityProvider.IIdentity
               ?? throw new InvalidOperationException(
                   "No identity on the request. This usually means it did not pass through the JWT middleware.");
    }
}
