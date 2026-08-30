using Microsoft.AspNetCore.Http;

namespace DcaShop.Checkout.Adapter.Incoming.Web;

/// <summary>
/// Reads the guest customer id the cart's web adapter issues (same cookie). Stage 1 has no Account context, so
/// this replaces the identity provider the Java sample consults; the two contexts share only the cookie name.
/// </summary>
public static class GuestCustomer
{
    public const string CookieName = "dcashop-customer";

    public static string? Identify(HttpContext context) =>
        context.Request.Cookies.TryGetValue(CookieName, out var id) && !string.IsNullOrWhiteSpace(id) ? id : null;
}
