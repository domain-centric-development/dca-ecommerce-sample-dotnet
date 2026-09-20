namespace DcaShop.Backoffice.Adapter.Incoming.Web;

/// <summary>
/// The operator account and the session it gets — cookie name, scheme and credentials of the backoffice web
/// adapter, so the class lives there; the module registration binds it from configuration. A single credential pair is enough for a sample; a real
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

    /// <summary>The operator name the sample ships with.</summary>
    public const string DevelopmentUsername = "admin";

    /// <summary>The operator password the sample ships with.</summary>
    /// <remarks>
    /// Committed, therefore known to everybody. The backoffice replays failed event publications, so this is the
    /// most privileged door in the shop; <c>BackofficeDevelopmentDefaultsValidator</c> refuses it outside the
    /// Development environment.
    /// </remarks>
    public const string DevelopmentPassword = "admin";

    public string Username { get; set; } = DevelopmentUsername;

    public string Password { get; set; } = DevelopmentPassword;

    /// <summary>
    /// Whether the session cookie is marked <c>Secure</c>. From configuration, never hardcoded — the same rule
    /// the shop's cookies follow (ADR-006).
    /// </summary>
    public bool SecureCookies { get; set; }

    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(8);
}
