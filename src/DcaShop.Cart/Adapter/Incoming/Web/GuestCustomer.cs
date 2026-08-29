using Microsoft.AspNetCore.Http;

namespace DcaShop.Cart.Adapter.Incoming.Web;

/// <summary>Identifies the browsing customer by a guest cookie until the Account context provides real identities.</summary>
public static class GuestCustomer
{
    public const string CookieName = "dcashop-customer";

    public static string IdentifyOrCreate(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(CookieName, out var existing) && !string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var id = "guest-" + Guid.NewGuid().ToString("N");
        context.Response.Cookies.Append(CookieName, id, new CookieOptions { HttpOnly = true, IsEssential = true, MaxAge = TimeSpan.FromDays(30) });
        return id;
    }
}
