using DcaShop.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DcaShop.Account.Adapter.Incoming.Security;

/// <summary>
/// Establishes and ends the authenticated session of the current browser by writing the two cookies of ADR-006 on
/// the current response. A collaborator of the Account context's web adapter — the login, registration, profile and
/// logout controllers — not a port: only this context modifies a session, and cookie mechanics are adapter business.
/// Request-scoped because it needs that response.
/// </summary>
public sealed class JwtIdentitySession
{
    private readonly JwtOptions _options;
    private readonly JwtTokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtIdentitySession(
        IOptions<JwtOptions> options, JwtTokenService tokenService, IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Starts an authenticated session for the given token and aligns the visitor identity with the account it
    /// names, so a later session expiry drops the browser onto the account's cart rather than a superseded one.
    /// </summary>
    public void SetRegisteredIdentity(string token)
    {
        var response = CurrentResponse();
        CookieWriter.Write(response, _options, _options.SessionCookieName, token, _options.SessionLifetime);

        // Align the identity with the account the session belongs to. Authenticating adopts the account's UserId,
        // which need not be the one the browser arrived with — and the anonymous cart is merged into the
        // account's cart and then deleted. Leaving the identity cookie on the superseded UserId would mean that
        // the next session expiry drops the browser onto a cart that no longer exists, which is exactly what
        // ADR-029 exists to prevent.
        if (_tokenService.ValidateAndParse(token) is { } identity)
        {
            CookieWriter.Write(
                response,
                _options,
                _options.IdentityCookieName,
                _tokenService.GenerateAnonymousToken(identity.UserId),
                _options.IdentityLifetime);
        }
    }

    /// <summary>
    /// Ends the session and rotates the visitor identity: the next person on a shared device must not inherit
    /// this cart, while the account's own cart returns at the next login.
    /// </summary>
    public void LogOut()
    {
        var response = CurrentResponse();
        CookieWriter.Delete(response, _options, _options.SessionCookieName);

        // Rotate rather than delete: the next person on a shared device must not inherit this cart, while the
        // account's own cart is restored on the next login (ADR-029).
        CookieWriter.Write(
            response,
            _options,
            _options.IdentityCookieName,
            _tokenService.GenerateAnonymousToken(UserId.GenerateAnonymous()),
            _options.IdentityLifetime);
    }

    private HttpResponse CurrentResponse() =>
        _httpContextAccessor.HttpContext?.Response
        ?? throw new InvalidOperationException("No HTTP context: an identity session exists only inside a request");
}
