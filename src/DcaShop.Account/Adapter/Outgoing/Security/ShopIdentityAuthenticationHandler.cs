using System.Text.Encodings.Web;
using DcaShop.Account.Application.Shared;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>Options of <see cref="ShopIdentityAuthenticationHandler"/>.</summary>
public sealed class ShopIdentityAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Whether this scheme reads the <c>Authorization: Bearer</c> header and nothing else. When
    /// <see langword="false"/> it reads the two cookies of ADR-006 and never the header.
    /// </summary>
    public bool BearerOnly { get; set; }

    /// <summary>Where a challenge on the cookie scheme sends the browser.</summary>
    public string LoginPath { get; set; } = "/login";
}

/// <summary>
/// Resolves the identity of every request into <c>HttpContext.User</c>, as an ASP.NET Core authentication handler.
/// Registered twice: once for the browser (cookies) and once for the API (Bearer header), selected by request path.
/// </summary>
/// <remarks>
/// <para>
/// It enriches, it does not gate (ADR-006): a request with an expired or forged session is not an error, it
/// proceeds as an anonymous visitor and sees what an anonymous visitor sees. Every request therefore ends with an
/// identity on it — recorded as <see cref="IShopIdentityFeature"/> for the port — but only a registered session
/// yields an <i>authenticated</i> principal on <c>HttpContext.User</c>. An anonymous visitor is no result for the
/// scheme, so <c>[Authorize]</c> challenges them (a redirect to the login form, or <c>401</c> on the API) instead
/// of forbidding them, and a registered caller without a role gets the <c>403</c> that distinction exists for.
/// </para>
/// <para>
/// The identity is resolved first and independently of authentication, because it carries the cart: an expired
/// or missing session must never cost it. A new <see cref="UserId"/> is minted only when the browser presents
/// none that can be read.
/// </para>
/// </remarks>
public sealed class ShopIdentityAuthenticationHandler : AuthenticationHandler<ShopIdentityAuthenticationOptions>
{
    private const string BearerPrefix = "Bearer ";

    private readonly JwtOptions _jwtOptions;
    private readonly JwtTokenService _tokenService;
    private readonly IRegisteredUserValidator _registeredUserValidator;

    public ShopIdentityAuthenticationHandler(
        IOptionsMonitor<ShopIdentityAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<JwtOptions> jwtOptions,
        JwtTokenService tokenService,
        IRegisteredUserValidator registeredUserValidator)
        : base(options, logger, encoder)
    {
        ArgumentNullException.ThrowIfNull(jwtOptions);
        _jwtOptions = jwtOptions.Value;
        _tokenService = tokenService;
        _registeredUserValidator = registeredUserValidator;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = Options.BearerOnly
            ? await ResolveBearerIdentityAsync().ConfigureAwait(false)
            : await ResolveSessionAsync(ResolveIdentity()).ConfigureAwait(false);

        Context.Features.Set<IShopIdentityFeature>(new ShopIdentityFeature(identity));

        if (!identity.IsRegistered)
        {
            // Not a failure: an anonymous visitor is simply not authenticated. Reporting a success with an
            // unauthenticated principal would make the policy evaluator forbid them rather than challenge them.
            return AuthenticateResult.NoResult();
        }

        var principal = ShopPrincipal.From(identity, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    /// <summary>
    /// An API caller without a usable token gets <c>401</c> and is told which scheme to use; a browser is sent to
    /// the login form and back to where it wanted to go. A registered caller lacking a role is not challenged but
    /// forbidden (<c>403</c>), which the base class renders.
    /// </summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (Options.BearerOnly)
        {
            Response.Headers[HeaderNames.WWWAuthenticate] = "Bearer";
            return ProblemAsync(StatusCodes.Status401Unauthorized);
        }

        var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
        Response.Redirect($"{Options.LoginPath}?returnUrl={Uri.EscapeDataString(returnUrl)}");
        return Task.CompletedTask;
    }

    /// <summary>A registered API caller without the required role. Pages keep the framework's bare <c>403</c>.</summary>
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        Options.BearerOnly ? ProblemAsync(StatusCodes.Status403Forbidden) : base.HandleForbiddenAsync(properties);

    /// <summary>
    /// An API refusal as an RFC 9457 problem document. Writing a body here also keeps the status-code pages
    /// middleware from re-executing the request onto the HTML error page.
    /// </summary>
    private Task ProblemAsync(int statusCode) => Results.Problem(statusCode: statusCode).ExecuteAsync(Context);

    /// <summary>
    /// Reads the visitor identity from its cookie, minting one only when the browser presents none that can be
    /// read. A token in this cookie is used for its <see cref="UserId"/> alone.
    /// </summary>
    private UserId ResolveIdentity()
    {
        if (ReadCookie(_jwtOptions.IdentityCookieName) is { } stored)
        {
            var validation = _tokenService.Validate(stored);
            if (validation is JwtTokenService.TokenValidation.Valid valid)
            {
                return valid.Identity.UserId;
            }

            Logger.LogDebug("Identity not usable ({Outcome}), issuing a new one", validation.GetType().Name);
        }

        // A valid session without an identity cookie: adopt the session's UserId rather than inventing a second
        // one that would contradict it.
        var userId = ReadCookie(_jwtOptions.SessionCookieName) is { } sessionToken
                     && _tokenService.Validate(sessionToken) is JwtTokenService.TokenValidation.Valid session
            ? session.Identity.UserId
            : UserId.GenerateAnonymous();

        CookieWriter.Write(
            Response,
            _jwtOptions,
            _jwtOptions.IdentityCookieName,
            _tokenService.GenerateAnonymousToken(userId),
            _jwtOptions.IdentityLifetime);
        return userId;
    }

    /// <summary>
    /// Resolves the authenticated session, falling back to an anonymous identity that keeps the browser's
    /// existing <see cref="UserId"/>. Every fallback is deliberately silent and non-blocking.
    /// </summary>
    private async Task<IIdentityProvider.IIdentity> ResolveSessionAsync(UserId identityUserId)
    {
        if (ReadCookie(_jwtOptions.SessionCookieName) is not { } token
            || _tokenService.Validate(token) is not JwtTokenService.TokenValidation.Valid valid
            || !valid.Identity.IsRegistered)
        {
            return JwtIdentity.Anonymous(identityUserId);
        }

        // The token is self-contained, so it outlives the account it names: a deleted account leaves a session
        // that still validates and still carries roles.
        if (!await _registeredUserValidator
                .ExistsForUserIdAsync(valid.Identity.UserId, Context.RequestAborted)
                .ConfigureAwait(false))
        {
            Logger.LogInformation("Session has no account, continuing anonymously");
            return JwtIdentity.Anonymous(identityUserId);
        }

        return valid.Identity;
    }

    /// <summary>
    /// The identity for a token-only endpoint: whatever the Bearer token says, or a throwaway anonymous identity
    /// when there is none. No cookie is read and none is issued — that is what makes the antiforgery exemption
    /// sound, and it is why a cross-site form post to the API arrives as a stranger.
    /// </summary>
    private async Task<IIdentityProvider.IIdentity> ResolveBearerIdentityAsync()
    {
        if (BearerToken() is not { } token
            || _tokenService.Validate(token) is not JwtTokenService.TokenValidation.Valid valid)
        {
            return JwtIdentity.Anonymous(UserId.GenerateAnonymous());
        }

        if (valid.Identity.IsRegistered
            && !await _registeredUserValidator
                .ExistsForUserIdAsync(valid.Identity.UserId, Context.RequestAborted)
                .ConfigureAwait(false))
        {
            return JwtIdentity.Anonymous(valid.Identity.UserId);
        }

        return valid.Identity;
    }

    private string? ReadCookie(string name) =>
        Request.Cookies.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private string? BearerToken() =>
        Request.Headers[HeaderNames.Authorization].ToString() is { } header
        && header.StartsWith(BearerPrefix, StringComparison.Ordinal)
            ? header[BearerPrefix.Length..]
            : null;
}
