using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A shop restart through the real HTTP pipeline. The restarted shop is a second one with a store of its own: the
/// same configuration, so the same signing secret and a session cookie that still validates, but no account. One
/// browser — one cookie jar — visits both, as it would visit the shop before and after the restart.
/// </summary>
public sealed class ShopRestartTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Email = "anna.schmidt@example.com";
    private const string Password = "Secret123";

    private readonly WebApplicationFactory<Program> _factory;

    public ShopRestartTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnAccountPageSendsTheFormerlyLoggedInBrowserToTheLoginAfterARestart()
    {
        var cookies = new CookieContainer();
        var beforeRestart = Browser(_factory.WithWebHostBuilder(_ => { }), cookies);
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(beforeRestart)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await beforeRestart.GetAsync("/account")).StatusCode);
        var afterRestart = Browser(_factory.WithWebHostBuilder(_ => { }), cookies);

        var response = await afterRestart.GetAsync("/account");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login?returnUrl=%2Faccount", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task TheCredentialsOfAnAccountFromBeforeARestartAreRefused()
    {
        Assert.Equal(
            HttpStatusCode.Redirect,
            (await RegisterAsync(Browser(_factory.WithWebHostBuilder(_ => { }), new CookieContainer()))).StatusCode);
        var visitor = Browser(_factory.WithWebHostBuilder(_ => { }), new CookieContainer());
        var page = await visitor.GetStringAsync("/login");

        var response = await visitor.PostAsync(
            "/login", Submission(Element(page, "login-form", "</form>"), ("email", Email), ("password", Password)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "Invalid email or password",
            Text(await response.Content.ReadAsStringAsync(), @"data-test=""login-error-message"">([^<]*)</div>"));
    }

    /// <summary>
    /// A browser in <paramref name="shop"/> that keeps its cookies in <paramref name="cookies"/>; redirects are not
    /// followed — each one is an assertion.
    /// </summary>
    private static HttpClient Browser(WebApplicationFactory<Program> shop, CookieContainer cookies) =>
        shop.CreateDefaultClient(new CookieContainerHandler(cookies));

    /// <summary>Opens the registration page and submits it with Anna's details.</summary>
    private static async Task<HttpResponseMessage> RegisterAsync(HttpClient client)
    {
        var page = await client.GetStringAsync("/register");
        return await client.PostAsync(
            "/register",
            Submission(
                Element(page, "register-form", "</form>"),
                ("firstName", "Anna"),
                ("lastName", "Schmidt"),
                ("dateOfBirth", "1990-05-17"),
                ("email", Email),
                ("password", Password),
                ("confirmPassword", Password)));
    }

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

    private static string Text(string markup, string pattern)
    {
        var match = Regex.Match(markup, pattern);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
    }
}