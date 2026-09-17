using System.Text.Encodings.Web;
using DcaShop.Account.Adapter.Outgoing.Security;
using DcaShop.Account.Application.IsAccountRegistered;
using DcaShop.SharedKernel.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DcaShop.UnitTests.Account;

/// <summary>
/// Pins the rule that a session ending must never cost the visitor identity (ADR-006/ADR-029): the cart is keyed on
/// that identity, so minting a fresh one silently orphans it. The account check goes through the
/// <c>IsAccountRegistered</c> input port, so no repository is pulled into a handler test. Mirrors the cases of the
/// Java <c>JwtAuthenticationFilterTest</c> by name.
/// </summary>
public sealed class ShopIdentityAuthenticationHandlerTest
{
    private const string Secret = "test-only-secret-key-must-be-at-least-256-bits-long-for-hmac-sha256";
    private const string IdentityCookie = "shop-identity";
    private const string SessionCookie = "shop-session";
    private const string Email = "jane.doe@example.com";

    private readonly JwtOptions _options = new()
    {
        Secret = Secret,
        Issuer = "test-issuer",
        IdentityCookieName = IdentityCookie,
        SessionCookieName = SessionCookie,
    };

    private readonly JwtTokenService _tokenService;
    private readonly TestIsAccountRegistered _accounts = new();

    public ShopIdentityAuthenticationHandlerTest() =>
        _tokenService = new JwtTokenService(Options.Create(_options), NullLogger<JwtTokenService>.Instance);

    [Fact]
    public async Task ValidSessionIsRegistered()
    {
        var visitor = UserId.GenerateAnonymous();

        var identity = await RunHandlerAsync(IdentityCookieFor(visitor), SessionCookieFor(visitor));

        Assert.True(identity.IsRegistered);
        Assert.Equal(visitor, identity.UserId);
        Assert.Equal(Email, identity.Email);
    }

    [Fact]
    public async Task SessionWithoutAccountKeepsIdentity()
    {
        var visitor = UserId.GenerateAnonymous();
        var session = SessionCookieFor(visitor);
        _accounts.Forget(visitor);

        var identity = await RunHandlerAsync(IdentityCookieFor(visitor), session);

        Assert.False(identity.IsRegistered);
        Assert.Equal(visitor, identity.UserId);
    }

    [Fact]
    public async Task ExpiredSessionKeepsTheVisitorIdentity()
    {
        var visitor = UserId.GenerateAnonymous();

        var identity = await RunHandlerAsync(IdentityCookieFor(visitor));

        Assert.False(identity.IsRegistered);
        Assert.Equal(visitor, identity.UserId);
    }

    private async Task<IIdentityProvider.IIdentity> RunHandlerAsync(params (string Name, string Value)[] cookies)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/products";
        if (cookies.Length > 0)
        {
            context.Request.Headers.Cookie = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
        }

        var handler = new ShopIdentityAuthenticationHandler(
            new StaticOptionsMonitor(new ShopIdentityAuthenticationOptions { BearerOnly = false }),
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            Options.Create(_options),
            _tokenService,
            _accounts);
        var scheme = new AuthenticationScheme(
            ShopPrincipal.CookieScheme, "Shop cookies", typeof(ShopIdentityAuthenticationHandler));
        await handler.InitializeAsync(scheme, context);

        var result = await handler.AuthenticateAsync();
        var identity = context.Features.Get<IShopIdentityFeature>()?.Identity;

        Assert.NotNull(identity);
        Assert.Equal(identity.IsRegistered, result.Succeeded);
        return identity;
    }

    private (string, string) IdentityCookieFor(UserId userId) =>
        (IdentityCookie, _tokenService.GenerateAnonymousToken(userId));

    private (string, string) SessionCookieFor(UserId userId)
    {
        _accounts.Register(userId);
        return (SessionCookie, _tokenService.GenerateRegisteredToken(userId, Email, new HashSet<string> { "CUSTOMER" }));
    }

    /// <summary>Test double for the account-registered query, so no repository is pulled into a handler test.</summary>
    private sealed class TestIsAccountRegistered : IIsAccountRegisteredInputPort
    {
        private readonly HashSet<string> _known = new();

        public void Register(UserId userId) => _known.Add(userId.Value);

        public void Forget(UserId userId) => _known.Remove(userId.Value);

        public Task<IsAccountRegisteredResult> ExecuteAsync(
            IsAccountRegisteredQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new IsAccountRegisteredResult(_known.Contains(query.UserId)));
    }

    private sealed class StaticOptionsMonitor : IOptionsMonitor<ShopIdentityAuthenticationOptions>
    {
        public StaticOptionsMonitor(ShopIdentityAuthenticationOptions options) => CurrentValue = options;

        public ShopIdentityAuthenticationOptions CurrentValue { get; }

        public ShopIdentityAuthenticationOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<ShopIdentityAuthenticationOptions, string?> listener) => null;
    }
}
