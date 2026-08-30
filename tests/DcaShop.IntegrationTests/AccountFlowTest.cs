using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Registration, login, logout and profile through the real HTTP pipeline, and with them the cookie design of
/// ADR-029/030: an expired session must not cost the visitor their identity, and only an explicit logout may
/// rotate it.
/// </summary>
public sealed class AccountFlowTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string IdentityCookie = "shop-identity";
    private const string SessionCookie = "shop-session";

    private readonly WebApplicationFactory<Program> _factory;

    public AccountFlowTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task RegisteringStartsASessionAndKeepsTheVisitorIdentity()
    {
        var (client, cookies) = NewBrowser();

        await client.GetAsync("/");
        var beforeIdentity = IdentityTokenOf(cookies);
        Assert.NotEmpty(beforeIdentity);
        var registered = await RegisterAsync(client, "keeps-identity@example.com");

        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);
        Assert.Equal("/", registered.Headers.Location!.ToString());
        Assert.Contains(SessionCookie, SetCookieNames(registered));

        // Registration adopts the identity the browser already had — that is what keeps a guest's cart.
        var account = await client.GetStringAsync("/account");
        Assert.Contains("data-test=\"account-overview\"", account, StringComparison.Ordinal);
        Assert.Equal(TokenSubject(beforeIdentity), TokenSubject(IdentityTokenOf(cookies)));
    }

    [Fact]
    public async Task AnAnonymousCartFollowsTheVisitorIntoTheirAccount()
    {
        // Fill a cart as a guest, then log in to an account that has none of its own.
        await RegisterAsync(Client(), "cart-recovery@example.com");
        var guest = Client();
        var catalog = await guest.GetStringAsync("/products");
        var productId = Regex.Match(catalog, @"href=""/products/([0-9a-f-]{36})""").Groups[1].Value;
        var detail = await guest.GetStringAsync($"/products/{productId}");
        await guest.PostAsync("/cart/add-product", Form(detail, ("productId", productId), ("quantity", "2")));

        var login = await guest.GetStringAsync("/login");
        var loggedIn = await guest.PostAsync(
            "/login", Form(login, ("email", "cart-recovery@example.com"), ("password", "Secret123")));

        Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);
        Assert.StartsWith("/cart/merge", loggedIn.Headers.Location!.ToString(), StringComparison.Ordinal);

        // Nothing to decide — but the items still have to arrive, or logging in would silently empty the cart.
        var merge = await guest.GetAsync(loggedIn.Headers.Location);
        Assert.Equal(HttpStatusCode.Redirect, merge.StatusCode);
        Assert.Equal("/cart", merge.Headers.Location!.ToString());

        var cart = await guest.GetStringAsync("/cart");
        Assert.Contains("data-test=\"cart-item\"", cart, StringComparison.Ordinal);
        Assert.Contains("data-test=\"mini-basket-count\">2<", cart, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AWrongPasswordIsRefusedWithoutSayingWhy()
    {
        var client = Client();
        await RegisterAsync(client, "wrong-password@example.com");

        var fresh = Client();
        var loginPage = await fresh.GetStringAsync("/login");
        var response = await fresh.PostAsync(
            "/login", Form(loginPage, ("email", "wrong-password@example.com"), ("password", "Wrong123x")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", body, StringComparison.Ordinal);
        Assert.DoesNotContain(SessionCookie, SetCookieNames(response));
    }

    [Fact]
    public async Task AnUnknownAddressIsRefusedWithTheSameMessage()
    {
        var client = Client();
        var loginPage = await client.GetStringAsync("/login");

        var response = await client.PostAsync(
            "/login", Form(loginPage, ("email", "nobody@example.com"), ("password", "Secret123")));

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoggingOutClearsTheSessionAndRotatesTheIdentity()
    {
        var (client, cookies) = NewBrowser();
        await RegisterAsync(client, "rotates@example.com");
        var identityWhileLoggedIn = TokenSubject(IdentityTokenOf(cookies));

        var account = await client.GetStringAsync("/account");
        var loggedOut = await client.PostAsync("/logout", Form(account));

        Assert.Equal(HttpStatusCode.Redirect, loggedOut.StatusCode);
        Assert.Equal("/login?logout=true", loggedOut.Headers.Location!.ToString());

        // The session is emptied and the identity is rotated, not deleted: the account's cart returns at the
        // next login, while the next person on this device inherits nothing.
        Assert.Contains(SetCookies(loggedOut), c => c.StartsWith($"{SessionCookie}=;", StringComparison.Ordinal));
        Assert.NotEqual(identityWhileLoggedIn, TokenSubject(IdentityTokenOf(cookies)));

        var afterLogout = await client.GetAsync("/account");
        Assert.Equal(HttpStatusCode.Redirect, afterLogout.StatusCode);
        Assert.StartsWith("/login", afterLogout.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnExpiredSessionLeavesTheIdentityAndTheCartIntact()
    {
        var (client, cookies) = NewBrowser();
        await RegisterAsync(client, "expiring@example.com");

        var catalog = await client.GetStringAsync("/products");
        var productId = Regex.Match(catalog, @"href=""/products/([0-9a-f-]{36})""").Groups[1].Value;
        var detail = await client.GetStringAsync($"/products/{productId}");
        await client.PostAsync("/cart/add-product", Form(detail, ("productId", productId), ("quantity", "1")));

        // A browser whose session aged out still presents its identity cookie. Nobody logged out, so the cart
        // must still be theirs (ADR-029).
        var expired = Client();
        expired.DefaultRequestHeaders.Add("Cookie", $"{IdentityCookie}={IdentityTokenOf(cookies)}");

        var cart = await expired.GetStringAsync("/cart");
        Assert.Contains("data-test=\"cart-item\"", cart, StringComparison.Ordinal);
        Assert.DoesNotContain("data-test=\"user-greeting\"", cart, StringComparison.Ordinal);
        Assert.Contains("data-test=\"nav-login-link\"", cart, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnAuthCookieIsHttpOnlyAndDeclaresItsSameSite()
    {
        var response = await Client().GetAsync("/");

        var identity = SetCookies(response).Single(c => c.StartsWith($"{IdentityCookie}=", StringComparison.Ordinal));
        Assert.Contains("httponly", identity, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", identity, StringComparison.OrdinalIgnoreCase);

        // Secure comes from configuration, so local HTTP development cannot bake it into a deployment.
        Assert.DoesNotContain("secure", identity, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoggingInWithoutAnAntiforgeryTokenIsRejected()
    {
        var response = await Client().PostAsync(
            "/login",
            new FormUrlEncodedContent([
                new KeyValuePair<string, string>("email", "someone@example.com"),
                new KeyValuePair<string, string>("password", "Secret123"),
            ]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangingTheProfileReissuesTheSessionUnderTheNewAddress()
    {
        var client = Client();
        await RegisterAsync(client, "before-change@example.com");

        var profile = await client.GetStringAsync("/account/profile");
        var saved = await client.PostAsync(
            "/account/profile", Form(profile, ("email", "after-change@example.com"), ("dateOfBirth", "1991-06-18")));

        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        Assert.Contains(SessionCookie, SetCookieNames(saved));

        var reloaded = await client.GetStringAsync("/account/profile");
        Assert.Contains("after-change@example.com", reloaded, StringComparison.Ordinal);
        Assert.Contains("Your profile has been updated.", reloaded, StringComparison.Ordinal);

        // The name is fixed at registration and is not part of the form at all.
        Assert.Contains("data-test=\"profile-name-note\"", reloaded, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ChangingThePasswordRequiresTheCurrentOne()
    {
        var client = Client();
        await RegisterAsync(client, "password-change@example.com");

        var page = await client.GetStringAsync("/account/change-password");
        var refused = await client.PostAsync(
            "/account/change-password",
            Form(page, ("currentPassword", "Wrong123x"), ("newPassword", "Another123"), ("confirmPassword", "Another123")));

        Assert.Contains(
            "Current password is not correct", await refused.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        page = await client.GetStringAsync("/account/change-password");
        var changed = await client.PostAsync(
            "/account/change-password",
            Form(page, ("currentPassword", "Secret123"), ("newPassword", "Another123"), ("confirmPassword", "Another123")));

        Assert.Equal(HttpStatusCode.Redirect, changed.StatusCode);

        // The new password is the one that logs in from now on.
        var fresh = Client();
        var loginPage = await fresh.GetStringAsync("/login");
        var loggedIn = await fresh.PostAsync(
            "/login", Form(loginPage, ("email", "password-change@example.com"), ("password", "Another123")));
        Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);
    }

    [Fact]
    public async Task AnAccountPageIsUnreachableWithoutASession()
    {
        var client = Client();

        foreach (var path in new[] { "/account", "/account/profile", "/account/change-password" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("/login?returnUrl=", response.Headers.Location!.ToString(), StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// A browser of its own: its cookies are visible to the test, because half of what ADR-029/030 promises is
    /// about which cookie survives which event. Redirects are not followed — each one is an assertion.
    /// </summary>
    private (HttpClient Browser, CookieContainer Cookies) NewBrowser()
    {
        var cookies = new CookieContainer();
        return (_factory.CreateDefaultClient(new CookieContainerHandler(cookies)), cookies);
    }

    private HttpClient Client() => NewBrowser().Browser;

    private static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email)
    {
        var page = await client.GetStringAsync("/register");
        return await client.PostAsync(
            "/register",
            Form(
                page,
                ("email", email),
                ("password", "Secret123"),
                ("confirmPassword", "Secret123"),
                ("firstName", "Jane"),
                ("lastName", "Doe"),
                ("dateOfBirth", "1990-05-17")));
    }

    /// <summary>The token in the identity cookie, which is what tells one visitor identity from another.</summary>
    private static string IdentityTokenOf(CookieContainer cookies) =>
        cookies.GetCookies(new Uri("http://localhost"))[IdentityCookie]?.Value ?? string.Empty;

    /// <summary>
    /// The subject of an identity token. Two tokens minted for the same visitor differ in their timestamps, so
    /// only the subject answers "is this still the same identity?".
    /// </summary>
    private static string TokenSubject(string token)
    {
        var payload = token.Split('.')[1];
        var padded = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/')));
        return Regex.Match(json, @"""sub""\s*:\s*""([^""]+)""").Groups[1].Value;
    }

    private static IEnumerable<string> SetCookies(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [];

    private static IEnumerable<string> SetCookieNames(HttpResponseMessage response) =>
        SetCookies(response).Select(c => c.Split('=')[0]);

    private static FormUrlEncodedContent Form(string renderedPage, params (string Name, string Value)[] fields)
    {
        var token = Regex
            .Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""")
            .Groups[1].Value;

        return new FormUrlEncodedContent(
            fields.Append(("__RequestVerificationToken", token))
                .Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}
