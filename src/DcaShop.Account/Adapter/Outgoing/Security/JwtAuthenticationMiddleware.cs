using DcaShop.Account.Application.Shared;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DcaShop.Account.Adapter.Outgoing.Security;

/// <summary>
/// Resolves the identity of every request and puts it where <see cref="HttpContextIdentityProvider"/> can read
/// it.
/// </summary>
/// <remarks>
/// <para>
/// It enriches, it does not gate (ADR-029): a request with an expired or forged session is not an error, it
/// proceeds as anonymous and sees what an anonymous visitor sees. Authorization for a protected action is
/// enforced by that action.
/// </para>
/// <para>
/// The identity is resolved first and independently of authentication, because it carries the cart: an expired
/// or missing session must never cost it. A new <see cref="UserId"/> is minted only when the browser presents
/// none that can be read.
/// </para>
/// </remarks>
public sealed class JwtAuthenticationMiddleware
{
    /// <summary>Key the resolved identity is stored under in <see cref="HttpContext.Items"/>.</summary>
    internal const string IdentityItemKey = "dcashop.identity";

    /// <summary>
    /// The paths authenticated by an <c>Authorization: Bearer</c> header and nothing else. This list and the
    /// antiforgery exemption in <c>Program.cs</c> are two halves of one decision: these endpoints may skip the
    /// antiforgery token <b>only</b> because no browser cookie can authenticate them. Changing one without the
    /// other is the mistake to watch for in review (ADR-007).
    /// </summary>
    public static readonly string[] TokenOnlyPathPrefixes = ["/api/", "/mcp"];

    private const string AuthorizationHeader = "Authorization";
    private const string BearerPrefix = "Bearer ";

    private readonly RequestDelegate _next;
    private readonly JwtOptions _options;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    public JwtAuthenticationMiddleware(
        RequestDelegate next, IOptions<JwtOptions> options, ILogger<JwtAuthenticationMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context, JwtTokenService tokenService, IRegisteredUserValidator registeredUserValidator)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (IsStaticResource(context.Request.Path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (IsTokenOnlyEndpoint(context.Request.Path))
        {
            context.Items[IdentityItemKey] =
                await ResolveBearerIdentityAsync(context, tokenService, registeredUserValidator)
                    .ConfigureAwait(false);
        }
        else
        {
            var identityUserId = ResolveIdentity(context, tokenService);
            context.Items[IdentityItemKey] =
                await ResolveSessionAsync(context, tokenService, registeredUserValidator, identityUserId)
                    .ConfigureAwait(false);
        }

        await _next(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the visitor identity from its cookie, minting one only when the browser presents none that can be
    /// read. A token in this cookie is used for its <see cref="UserId"/> alone.
    /// </summary>
    private UserId ResolveIdentity(HttpContext context, JwtTokenService tokenService)
    {
        if (ReadCookie(context, _options.IdentityCookieName) is { } stored)
        {
            var validation = tokenService.Validate(stored);
            if (validation is JwtTokenService.TokenValidation.Valid valid)
            {
                return valid.Identity.UserId;
            }

            _logger.LogDebug("Identity not usable ({Outcome}), issuing a new one", validation.GetType().Name);
        }

        // A valid session without an identity cookie: adopt the session's UserId rather than inventing a second
        // one that would contradict it.
        var userId = SessionToken(context) is { } sessionToken
                     && tokenService.Validate(sessionToken) is JwtTokenService.TokenValidation.Valid session
            ? session.Identity.UserId
            : UserId.GenerateAnonymous();

        CookieWriter.Write(
            context.Response,
            _options,
            _options.IdentityCookieName,
            tokenService.GenerateAnonymousToken(userId),
            _options.IdentityLifetime);
        return userId;
    }

    /// <summary>
    /// Resolves the authenticated session, falling back to an anonymous identity that keeps the browser's
    /// existing <see cref="UserId"/>. Every fallback is deliberately silent and non-blocking.
    /// </summary>
    private async Task<IIdentityProvider.IIdentity> ResolveSessionAsync(
        HttpContext context,
        JwtTokenService tokenService,
        IRegisteredUserValidator registeredUserValidator,
        UserId identityUserId)
    {
        if (SessionToken(context) is not { } token
            || tokenService.Validate(token) is not JwtTokenService.TokenValidation.Valid valid
            || !valid.Identity.IsRegistered)
        {
            return JwtIdentity.Anonymous(identityUserId);
        }

        // The token is self-contained, so it outlives the account it names: a deleted account leaves a session
        // that still validates and still carries roles.
        if (!await registeredUserValidator
                .ExistsForUserIdAsync(valid.Identity.UserId, context.RequestAborted)
                .ConfigureAwait(false))
        {
            _logger.LogInformation("Session has no account, continuing anonymously");
            return JwtIdentity.Anonymous(identityUserId);
        }

        return valid.Identity;
    }

    /// <summary>
    /// The identity for a token-only endpoint: whatever the Bearer token says, or a throwaway anonymous identity
    /// when there is none. No cookie is read and none is issued — that is what makes the antiforgery exemption
    /// sound, and it is why a cross-site form post to the API arrives as a stranger.
    /// </summary>
    private static async Task<IIdentityProvider.IIdentity> ResolveBearerIdentityAsync(
        HttpContext context, JwtTokenService tokenService, IRegisteredUserValidator registeredUserValidator)
    {
        if (BearerToken(context) is not { } token
            || tokenService.Validate(token) is not JwtTokenService.TokenValidation.Valid valid)
        {
            return JwtIdentity.Anonymous(UserId.GenerateAnonymous());
        }

        if (valid.Identity.IsRegistered
            && !await registeredUserValidator
                .ExistsForUserIdAsync(valid.Identity.UserId, context.RequestAborted)
                .ConfigureAwait(false))
        {
            return JwtIdentity.Anonymous(valid.Identity.UserId);
        }

        return valid.Identity;
    }

    /// <summary>
    /// Whether a path is authenticated by a Bearer token alone. The antiforgery filter asks the same question,
    /// which is what keeps the exemption and the cookie-free treatment in step.
    /// </summary>
    public static bool IsTokenOnlyEndpoint(PathString path) =>
        path.Value is { } value
        && TokenOnlyPathPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private string? SessionToken(HttpContext context) =>
        ReadCookie(context, _options.SessionCookieName) ?? BearerToken(context);

    private static string? ReadCookie(HttpContext context, string name) =>
        context.Request.Cookies.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static string? BearerToken(HttpContext context) =>
        context.Request.Headers[AuthorizationHeader].ToString() is { } header
        && header.StartsWith(BearerPrefix, StringComparison.Ordinal)
            ? header[BearerPrefix.Length..]
            : null;

    private static bool IsStaticResource(PathString path)
    {
        var value = path.Value;
        if (value is null)
        {
            return false;
        }

        return value.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("/fonts/", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);
    }
}
