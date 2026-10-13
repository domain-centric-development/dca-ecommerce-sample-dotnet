using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The account overview through the real HTTP pipeline: how the header leads there, what it shows of an account
/// that was never logged into, its navigation, and how a visitor without a session is sent to the login and back.
/// Each test that registers Anna does so in a shop with a store of its own.
/// </summary>
public sealed class MyAccountPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Email = "anna.schmidt@example.com";
    private const string Password = "Secret123";

    private readonly WebApplicationFactory<Program> _factory;

    public MyAccountPageTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheHeaderOfALoggedInCustomerOffersMyAccount()
    {
        var client = Visitor(_factory.WithWebHostBuilder(_ => { }));
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);

        var catalogue = await client.GetStringAsync("/products");

        var myAccount = Link(Element(catalogue, "site-header", "</header>"), "nav-my-account-link");
        Assert.Equal("My Account", myAccount.Text);
        Assert.Equal("/account", myAccount.Target);
    }

    [Fact]
    public async Task AnAccountNeverLoggedIntoShowsNeverAsItsLastLogin()
    {
        var client = Visitor(_factory.WithWebHostBuilder(_ => { }));
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);

        var overview = await client.GetStringAsync("/account");

        Assert.Equal(
            "Last Login",
            Text(overview, @"<span class=""account__detail-label"">([^<]*)</span>\s*<span[^>]*data-test=""account-last-login"""));
        Assert.Equal("Never", Text(overview, @"data-test=""account-last-login"">([^<]*)</span>"));
    }

    [Fact]
    public async Task TheAccountNavigationListsTheAccountPagesWithTheOverviewAsTheCurrentOne()
    {
        var client = Visitor(_factory.WithWebHostBuilder(_ => { }));
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);

        var overview = await client.GetStringAsync("/account");

        var items = NavigationItems(Element(overview, "account-nav", "</nav>"));
        Assert.Equal(["Overview", "My Profile", "Change Password", "My Orders"], items.Select(i => i.Label));
        Assert.Equal(["/account", "/account/profile", "/account/change-password"], items.Take(3).Select(i => i.Target));
        Assert.Equal(["Overview"], items.Where(i => i.Markup.Contains(@"aria-current=""page""", StringComparison.Ordinal)).Select(i => i.Label));
        Assert.Contains("account__nav-link--active", items[0].Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MyOrdersIsShownDisabledAndLeadsNowhere()
    {
        var client = Visitor(_factory.WithWebHostBuilder(_ => { }));
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);

        var overview = await client.GetStringAsync("/account");

        var orders = Assert.Single(NavigationItems(Element(overview, "account-nav", "</nav>")), i => i.Label == "My Orders");
        Assert.Equal("span", orders.Tag);
        Assert.Null(orders.Target);
        Assert.Contains(@"aria-disabled=""true""", orders.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AVisitorWithoutCookiesIsSentFromTheOverviewToTheLogin()
    {
        var client = Visitor(_factory);

        var response = await client.GetAsync("/account");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?returnUrl=%2Faccount", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task ALoginTheOverviewSentForReturnsToTheOverview()
    {
        var shop = _factory.WithWebHostBuilder(_ => { });
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(Visitor(shop), Email)).StatusCode);
        var client = Visitor(shop);
        var sent = await client.GetAsync("/account");
        var page = await client.GetStringAsync(sent.Headers.Location);

        var loggedIn = await client.PostAsync(
            "/login", Submission(Element(page, "login-form", "</form>"), ("email", Email), ("password", Password)));

        var landing = await FollowRedirectsAsync(client, loggedIn);
        Assert.Equal("/account", landing.Path);
        Assert.Equal(
            $"Welcome, {Email}",
            Text(await landing.Response.Content.ReadAsStringAsync(), @"data-test=""account-greeting"">([^<]*)</h2>"));
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
            Submission(
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

    /// <summary>The entries of the account navigation in display order: the element, its label and its target.</summary>
    private static List<(string Tag, string Label, string? Target, string Markup)> NavigationItems(string nav) =>
        [.. Regex.Matches(nav, @"<(a|span)\b([^>]*data-test=""account-nav-[^""]+""[^>]*)>([^<]*)</\1>")
            .Select(m =>
            {
                var href = Regex.Match(m.Groups[2].Value, @"href=""([^""]*)""");
                return (
                    m.Groups[1].Value,
                    Rendered(m.Groups[3].Value),
                    href.Success ? WebUtility.HtmlDecode(href.Groups[1].Value) : null,
                    m.Groups[2].Value);
            })];

    /// <summary>The form as the browser would send it: its hidden fields as rendered, plus what the visitor typed.</summary>
    private static FormUrlEncodedContent Submission(string form, params (string Name, string Value)[] typed)
    {
        var hidden = Regex.Matches(form, @"<input\b[^>]*type=""hidden""[^>]*>")
            .Select(input => (
                Name: Regex.Match(input.Value, @"name=""([^""]+)""").Groups[1].Value,
                Value: WebUtility.HtmlDecode(Regex.Match(input.Value, @"value=""([^""]*)""").Groups[1].Value)));

        return new FormUrlEncodedContent(
            hidden.Concat(typed).Select(f => new KeyValuePair<string, string>(f.Name, f.Value)));
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