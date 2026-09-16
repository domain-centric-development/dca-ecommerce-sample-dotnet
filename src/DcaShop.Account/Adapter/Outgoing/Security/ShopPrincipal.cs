using System.Security.Claims;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>
/// The scheme names of the shop's identity and the mapping from the port's <see cref="IIdentityProvider.IIdentity"/>
/// to the <see cref="ClaimsPrincipal"/> ASP.NET Core carries on <c>HttpContext.User</c>.
/// </summary>
/// <remarks>
/// <see cref="ClaimsPrincipal"/> is the framework's currency: <c>[Authorize]</c>, the authorization policies and the
/// challenge/forbid pipeline all read it. The port is the application's currency and is served from
/// <see cref="IShopIdentityFeature"/>, not read back out of the principal. This class is the only place where the
/// one becomes the other (ADR-008).
/// </remarks>
public static class ShopPrincipal
{
    /// <summary>The default scheme: a policy scheme that forwards to one of the two below by request path.</summary>
    public const string Scheme = "Shop";

    /// <summary>The browser scheme: the two cookies of ADR-006.</summary>
    public const string CookieScheme = "ShopCookies";

    /// <summary>The API scheme: an <c>Authorization: Bearer</c> header and nothing else (ADR-007).</summary>
    public const string BearerScheme = "ShopBearer";

    /// <summary>Claim carrying the <see cref="IIdentityProvider.IdentityType"/>.</summary>
    public const string IdentityTypeClaim = "dcashop:identity-type";

    private const string TypeAnonymous = "anonymous";
    private const string TypeRegistered = "registered";

    /// <summary>
    /// Builds the authenticated principal for a registered identity: <c>NameIdentifier</c> carries the
    /// <see cref="UserId"/>, <c>Role</c> the roles <c>[Authorize(Roles = …)]</c> asks for.
    /// </summary>
    public static ClaimsPrincipal From(IIdentityProvider.IIdentity identity, string authenticatedScheme)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, identity.UserId.Value),
            new(IdentityTypeClaim, identity.IsRegistered ? TypeRegistered : TypeAnonymous),
        };
        if (identity.Email is { } email)
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        claims.AddRange(identity.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var claimsIdentity = new ClaimsIdentity(claims, authenticatedScheme, ClaimTypes.NameIdentifier, ClaimTypes.Role);
        return new ClaimsPrincipal(claimsIdentity);
    }
}
