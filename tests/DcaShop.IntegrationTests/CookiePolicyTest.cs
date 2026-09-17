using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The cookie and framing policy of the two deployments. A normal shop keeps its cookies same-site and refuses to be
/// framed; the embedded shop — the demo inside an iframe on another origin — needs every cookie it relies on to be
/// <c>SameSite=None; Secure</c>, the identity and the antiforgery token alike, or the request arrives anonymous or
/// without its form token.
///
/// Same two scenarios as the Java sample's <c>CookiePolicyIntegrationTest</c> and
/// <c>EmbeddedShopCookiePolicyIntegrationTest</c>.
/// </summary>
public sealed class CookiePolicyTest : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Regex ProductLink = new(@"href=""/products/([0-9a-f-]{36})""", RegexOptions.Compiled);

    private readonly WebApplicationFactory<Program> _factory;

    public CookiePolicyTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ANormalShopKeepsItsCookiesSameSiteAndRefusesToBeFramed()
    {
        var client = _factory.CreateClient();

        var cookies = await SetCookieHeadersOfAPageWithAFormAsync(client);

        var identity = Required(cookies, CookiePolicy.Named(cookies, "shop-identity"), "shop-identity");
        AssertSameSite("Lax", identity);
        Assert.False(identity.Secure);
        Assert.True(identity.HttpOnly);

        var antiforgery = Required(cookies, CookiePolicy.NamedByPrefix(cookies, ".AspNetCore.Antiforgery."), "antiforgery");
        AssertSameSite("Lax", antiforgery);

        var products = await client.GetAsync("/products");
        Assert.Equal("SAMEORIGIN", products.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task AnEmbeddedShopLetsEveryCookieTravelIntoAForeignFrame()
    {
        var client = EmbeddedShopClient();

        var cookies = await SetCookieHeadersOfAPageWithAFormAsync(client);

        var identity = Required(cookies, CookiePolicy.Named(cookies, "shop-identity"), "shop-identity");
        AssertSameSite("None", identity);
        Assert.True(identity.Secure);

        var antiforgery = Required(cookies, CookiePolicy.NamedByPrefix(cookies, ".AspNetCore.Antiforgery."), "antiforgery");
        AssertSameSite("None", antiforgery);
        Assert.True(antiforgery.Secure);
    }

    [Fact]
    public async Task AnEmbeddedShopMayBeFramedByAnotherOrigin()
    {
        var client = EmbeddedShopClient();

        var products = await client.GetAsync("/products");

        Assert.False(products.Headers.Contains("X-Frame-Options"));
    }

    /// <summary>A cookie the policy is about — with the whole Set-Cookie list in the message when it is missing.</summary>
    private static CookiePolicy Required(IReadOnlyList<string> cookies, CookiePolicy? cookie, string what) =>
        cookie ?? throw new Xunit.Sdk.XunitException($"no {what} cookie among: {string.Join(" | ", cookies)}");

    /// <summary>The attribute is case-insensitive on the wire: ASP.NET Core writes <c>samesite=none</c>.</summary>
    private static void AssertSameSite(string expected, CookiePolicy cookie) =>
        Assert.Equal(expected, cookie.SameSite, ignoreCase: true);

    /// <summary>The shop as a demo inside a foreign iframe — the one deployment that sets <c>SameSite=None</c>.</summary>
    private WebApplicationFactory<Program> EmbeddedShop() =>
        _factory.WithWebHostBuilder(builder => builder
            .UseSetting("Jwt:SameSite", "None")
            .UseSetting("Jwt:SecureCookies", "true"));

    /// <summary>
    /// The embedded shop is reached over TLS: <c>SameSite=None</c> needs <c>Secure</c>, a browser drops a Secure
    /// cookie that did not arrive over HTTPS, and ASP.NET Core refuses to issue an antiforgery token at all when its
    /// cookie is Secure and the request is not. Embedding the shop therefore means terminating TLS in front of it.
    /// </summary>
    private HttpClient EmbeddedShopClient() =>
        EmbeddedShop().CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    /// <summary>
    /// The <c>Set-Cookie</c> headers of the product page — it carries the add-to-cart form, and the antiforgery
    /// cookie is written only for a response that actually renders a token.
    /// </summary>
    private static async Task<IReadOnlyList<string>> SetCookieHeadersOfAPageWithAFormAsync(HttpClient client)
    {
        // Both responses of the visit: the client keeps the cookies it was handed, so the identity arrives with the
        // catalog and only the antiforgery token is set again on the page that renders the form.
        var catalog = await client.GetAsync("/products");
        var productId = ProductLink.Match(await catalog.Content.ReadAsStringAsync()).Groups[1].Value;
        Assert.NotEmpty(productId);
        var detail = await client.GetAsync($"/products/{productId}");

        if (detail.StatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new Xunit.Sdk.XunitException(
                $"product page answered {(int)detail.StatusCode}: {(await detail.Content.ReadAsStringAsync())[..Math.Min(1500, (await detail.Content.ReadAsStringAsync()).Length)]}");
        }
        var cookies = SetCookieHeadersOf(catalog).Concat(SetCookieHeadersOf(detail)).ToList();
        Assert.NotEmpty(cookies);
        return cookies;
    }

    private static IEnumerable<string> SetCookieHeadersOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [];
}
