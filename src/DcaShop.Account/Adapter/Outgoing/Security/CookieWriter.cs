using Microsoft.AspNetCore.Http;

namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>
/// The one place that writes an auth cookie. Cookie hardening is not optional (ADR-030): <c>HttpOnly</c> always,
/// <c>Secure</c> driven by configuration and never hardcoded, <c>SameSite</c> explicit on every cookie.
/// </summary>
internal static class CookieWriter
{
    public static void Write(HttpResponse response, JwtOptions options, string name, string value, TimeSpan maxAge) =>
        response.Cookies.Append(name, value, OptionsFor(options, maxAge));

    /// <summary>Ends a cookie by handing the browser an empty value that expires immediately.</summary>
    public static void Delete(HttpResponse response, JwtOptions options, string name) =>
        response.Cookies.Append(name, string.Empty, OptionsFor(options, TimeSpan.Zero));

    private static CookieOptions OptionsFor(JwtOptions options, TimeSpan maxAge) => new()
    {
        HttpOnly = true,
        Secure = options.SecureCookies,
        SameSite = JwtOptions.SameSite,
        Path = "/",
        MaxAge = maxAge,

        // The cart depends on the identity cookie, so it is strictly necessary under ePrivacy and must not be
        // suppressed by a consent decision (ADR-030).
        IsEssential = true,
    };
}
