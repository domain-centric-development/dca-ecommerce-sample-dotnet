namespace DcaShop.Backoffice.Infrastructure;

/// <summary>
/// The operator account and the session it gets. A single credential pair is enough for a sample; a real
/// deployment would put operators behind the same account store as everyone else and give them a role.
/// </summary>
public sealed class BackofficeOptions
{
    public const string SectionName = "Backoffice";

    /// <summary>
    /// The authentication scheme, separate from the shop's. An operator session and a shopper session must never
    /// be the same cookie: a staff credential is a different kind of token from a customer's, and mixing them
    /// would let one grant the other.
    /// </summary>
    public const string AuthenticationScheme = "Backoffice";

    public const string CookieName = "backoffice-session";

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = "admin";

    /// <summary>
    /// Whether the session cookie is marked <c>Secure</c>. From configuration, never hardcoded — the same rule
    /// the shop's cookies follow (ADR-006).
    /// </summary>
    public bool SecureCookies { get; set; }

    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(8);
}
