
using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The login page and the logout through the real HTTP pipeline, each test as a browser with no cookies: how the
/// header leads there, what the form asks for, where a login opened for another page leads, and what a logout says.
/// </summary>
public sealed class LoginPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Secret123";

    private readonly WebApplicationFactory<Program> _factory;

    public LoginPageTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheHeaderOfTheCatalogueOffersALoginLinkNextToTheRegisterLink()
    {
        var client = Visitor(_factory);

        var catalogue = await client.GetStringAsync("/products");

        var header = Element(catalogue, "site-header", "</header>");
        var login = Link(header, "nav-login-link");
        Assert.Equal("Login", login.Text);
        Assert.Equal("/login", login.Target);
        Assert.Matches(@"data-test=""nav-login-link"">[^<]*</a>\s*<a\b[^>]*data-test=""nav-register-link""", header);
    }

    [Fact]
    public async Task TheLoginPageAsksForEmailAndPasswordAndLeadsToTheRegistration()
    {
        var client = Visitor(_factory);

        var page = await client.GetStringAsync("/login");

        Assert.Equal("Login", Text(page, @"class=""auth__title"">([^<]*)</h2>"));
        var form = Element(page, "login-form", "</form>");
        Assert.Equal(
            ["Email", "Password"],
            Regex.Matches(form, @"<label\b[^>]*>([^<]*)</label>").Select(m => Rendered(m.Groups[1].Value)));
        foreach (Match label in Regex.Matches(form, @"<label\b[^>]*for=""([^""]+)"""))
        {
            Assert.Matches($@"<input\b[^>]*id=""{Regex.Escape(label.Groups[1].Value)}""", form);
        }
        Assert.Equal("Login", Text(form, @"data-test=""login-submit-button"">([^<]*)</button>"));
        var register = Link(page, "login-register-link");
        Assert.Equal("Register here", register.Text);
        Assert.Equal("/register", register.Target);
    }

    [Fact]
    public async Task ALoginOpenedForTheCatalogueReturnsThere()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(Visitor(shop), "anna.schmidt@example.com")).StatusCode);
        var client = Visitor(shop);
        var page = await client.GetStringAsync("/login?returnUrl=/products");

        var loggedIn = await client.PostAsync(
            "/login",
            Forms.AsRendered(
                Element(page, "login-form", "</form>"), ("email", "anna.schmidt@example.com"), ("password", Password)));

        var landing = await FollowRedirectsAsync(client, loggedIn);
        Assert.Equal("/products", landing.Path);
        Assert.Equal(HttpStatusCode.OK, landing.Response.StatusCode);
    }

    [Fact]
    public async Task TheRegisterLinkOfALoginOpenedForTheCatalogueKeepsTheCatalogue()
    {
        var client = Visitor(_factory);

        var page = await client.GetStringAsync("/login?returnUrl=/products");

        var register = Link(page, "login-register-link");
        Assert.Equal("/register?returnUrl=/products", Uri.UnescapeDataString(register.Target));
    }

    [Fact]
    public async Task LoggingOutOpensTheLoginPageWithTheConfirmationAndTheHeaderOffersLoginAgain()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        var client = Visitor(shop);
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, "anna.schmidt@example.com")).StatusCode);
        var catalogue = await client.GetStringAsync("/products");

        var loggedOut = await client.PostAsync(
            "/logout", Forms.AsRendered(Element(catalogue, "logout-form", "</form>")));

        Assert.Equal(HttpStatusCode.Redirect, loggedOut.StatusCode);
        Assert.Equal("/login?logout=true", loggedOut.Headers.Location!.ToString());
        var login = await client.GetStringAsync(loggedOut.Headers.Location);
        Assert.Equal(
            "You have been successfully logged out.",
            Text(login, @"data-test=""logout-success-message"">([^<]*)</div>"));
        var header = Element(login, "site-header", "</header>");
        Assert.Equal("Login", Link(header, "nav-login-link").Text);
        Assert.Equal("Register", Link(header, "nav-register-link").Text);
        Assert.DoesNotContain(@"data-test=""user-greeting""", header, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnAddressWithoutADomainEndingIsRefusedWithTheSameMessageAsAWrongPassword()
    {
        var client = Visitor(_factory);
        var page = await client.GetStringAsync("/login");

        var refused = await client.PostAsync(
            "/login",
            Forms.AsRendered(Element(page, "login-form", "</form>"), ("email", "anna.schmidt@example"), ("password", Password)));

        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        var answer = await refused.Content.ReadAsStringAsync();
        Assert.Equal("Invalid email or password", Text(answer, @"data-test=""login-error-message"">([^<]*)</div>"));
    }

    /// <summary>
    /// A browser of its own in <paramref name="shop"/>, with no cookies yet; it keeps the cookies it is given, and
    /// redirects are not followed — each one is an assertion.
    /// </summary>
    private static HttpClient Visitor(WebApplicationFactory<Program> shop) =>
        shop.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Opens the registration page and submits it with Anna's details under <paramref name="email"/>.</summary>
    private static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email)
    {
        var page = await client.GetStringAsync("/register");
        return await client.PostAsync(
            "/register",
            Forms.AsRendered(
                Element(page, "register-form", "</form>"),
                ("firstName", "Anna"),
                ("lastName", "Schmidt"),
                ("dateOfBirth", "1990-05-17"),
                ("email", email),
                ("password", Password),
                ("confirmPassword", Password)));
    }

    /// <summary>Follows the redirects of <paramref name="response"/> as a browser would; answers where it ends.</summary>
    private static async Task<(string Path, HttpResponseMessage Response)> FollowRedirectsAsync(
        HttpClient client, HttpResponseMessage response)
    {
        var path = string.Empty;
        for (var hops = 0; response.StatusCode == HttpStatusCode.Redirect && hops < 5; hops++)
        {
            path = response.Headers.Location!.ToString();
            response = await client.GetAsync(path);
        }

        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
        return (path, response);
    }

    /// <summary>The markup from the element carrying <paramref name="dataTest"/> to its closing tag.</summary>
    private static string Element(string page, string dataTest, string closingTag)
    {
        var start = page.IndexOf($"data-test=\"{dataTest}\"", StringComparison.Ordinal);
        Assert.True(start >= 0, $"the page has a {dataTest}");
        return page[start..].Split(closingTag)[0];
    }

    /// <summary>The target and text of the anchor carrying <paramref name="dataTest"/>.</summary>
    private static (string Target, string Text) Link(string markup, string dataTest)
    {
        var match = Regex.Match(markup, $@"<a\b[^>]*href=""([^""]+)""[^>]*data-test=""{Regex.Escape(dataTest)}"">([^<]*)</a>");
        Assert.True(match.Success, $"the markup offers a {dataTest}");
        return (WebUtility.HtmlDecode(match.Groups[1].Value), Rendered(match.Groups[2].Value));
    }

    private static string Text(string markup, string pattern)
    {
        var match = Regex.Match(markup, pattern);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return Rendered(match.Groups[1].Value);
    }

    /// <summary>The text a reader sees: tags removed, entities decoded.</summary>
    private static string Rendered(string html) => WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", string.Empty)).Trim();
}