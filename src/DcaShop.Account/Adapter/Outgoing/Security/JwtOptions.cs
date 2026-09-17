namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>
/// Configuration of the token and cookie design of ADR-029/030: one cookie carries the visitor identity the cart
/// is keyed on, a second carries the authenticated session, and the two have different lifetimes.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Configuration section these options are bound from.</summary>
    public const string SectionName = "Jwt";

    public const string DefaultIdentityCookieName = "shop-identity";

    public const string DefaultSessionCookieName = "shop-session";

    public const string DefaultIssuer = "dca-ecommerce-sample";

    /// <summary>Explicit on every cookie the subsystem sets, as defence in depth beside the antiforgery token.</summary>
    public const Microsoft.AspNetCore.Http.SameSiteMode DefaultSameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;

    /// <summary>The HS256 signing secret. At least 32 characters (256 bits).</summary>
    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = DefaultIssuer;

    /// <summary>Lifetime of the visitor identity. It outlives the session on purpose.</summary>
    public int AnonymousExpirationDays { get; set; } = 30;

    /// <summary>Lifetime of the authenticated session. Without a refresh token this is the blast radius of a
    /// stolen token, so it is deliberately the shorter of the two (ADR-030).</summary>
    public int RegisteredExpirationDays { get; set; } = 7;

    public string IdentityCookieName { get; set; } = DefaultIdentityCookieName;

    public string SessionCookieName { get; set; } = DefaultSessionCookieName;

    /// <summary>
    /// Whether the cookies are marked <c>Secure</c>. It comes from configuration so that local HTTP development
    /// cannot bake <see langword="false"/> into a deployment (ADR-030).
    /// </summary>
    public bool SecureCookies { get; set; }

    /// <summary>
    /// The <c>SameSite</c> policy of both cookies. <c>Lax</c> unless configured otherwise. The one reason to set
    /// <c>None</c> is running the shop inside an iframe on another origin — a demo or a presentation — where the
    /// browser withholds a <c>Lax</c> cookie, so every request arrives without an identity and the cart is never
    /// found. That widens the CSRF surface, which is why it is opt-in and never the default. Browsers also reject
    /// <c>None</c> without <c>Secure</c>; <see cref="Validate"/> refuses the combination rather than letting the
    /// browser drop the cookie silently.
    /// </summary>
    public Microsoft.AspNetCore.Http.SameSiteMode SameSite { get; set; } = DefaultSameSite;

    public TimeSpan IdentityLifetime => TimeSpan.FromDays(AnonymousExpirationDays);

    public TimeSpan SessionLifetime => TimeSpan.FromDays(RegisteredExpirationDays);

    /// <summary>Fails fast on a configuration that cannot hold the design up.</summary>
    public void Validate()
    {
        // Browsers drop a SameSite=None cookie that is not Secure — fail here rather than
        // let the shop look broken.
        if (SameSite == Microsoft.AspNetCore.Http.SameSiteMode.None && !SecureCookies)
        {
            throw new InvalidOperationException(
                "SameSite=None requires Secure cookies; browsers drop the cookie otherwise. "
                + "Set Jwt__SecureCookies=true.");
        }

        if (Secret is null || Secret.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be at least 32 characters (256 bits) for HS256");
        }

        if (AnonymousExpirationDays <= 0 || RegisteredExpirationDays <= 0)
        {
            throw new InvalidOperationException("JWT lifetimes must be positive");
        }

        if (string.IsNullOrWhiteSpace(IdentityCookieName) || string.IsNullOrWhiteSpace(SessionCookieName))
        {
            throw new InvalidOperationException("Both cookie names must be configured");
        }

        if (IdentityCookieName == SessionCookieName)
        {
            throw new InvalidOperationException(
                $"Identity and session must not share a cookie, see ADR-030: {IdentityCookieName}");
        }
    }
}
