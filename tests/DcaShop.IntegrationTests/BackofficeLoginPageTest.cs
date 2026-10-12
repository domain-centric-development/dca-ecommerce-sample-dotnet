using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The back office's own login page through the real HTTP pipeline, each test as a browser with no cookies: what the
/// form asks for, and what a login with wrong credentials leads to. The development run's operator is
/// <c>admin</c>/<c>admin</c>.
/// </summary>
public sealed class BackofficeLoginPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BackofficeLoginPageTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheLoginPageAsksForUsernameAndPasswordUnderItsOwnHeading()
    {
        var client = Browser();

        var page = await client.GetStringAsync("/backoffice/login");

        Assert.Equal("Backoffice Login", Text(page, @"class=""auth__title"">([^<]*)</h2>"));
        var form = Element(page, "backoffice-login-form", "</form>");
        Assert.Equal(
            ["Username", "Password"],
            Regex.Matches(form, @"<label\b[^>]*>([^<]*)</label>").Select(m => Rendered(m.Groups[1].Value)));
        foreach (Match label in Regex.Matches(form, @"<label\b[^>]*for=""([^""]+)"""))
        {
            Assert.Matches($@"<input\b[^>]*id=""{Regex.Escape(label.Groups[1].Value)}""", form);
        }
        Assert.Equal("Login", Text(form, @"data-test=""backoffice-login-submit"">([^<]*)</button>"));
    }

    [Fact]
    public async Task AWrongPasswordLeadsBackToTheLoginPageWithTheMessage()
    {
        var client = Browser();

        var refused = await LogInAsync(client, "admin", "wrong");

        Assert.Equal(HttpStatusCode.Redirect, refused.StatusCode);
        Assert.Equal("/backoffice/login?error=true", refused.Headers.Location!.ToString());
        var page = await client.GetStringAsync(refused.Headers.Location);
        Assert.Equal("Invalid username or password.", Text(page, @"data-test=""login-error-message"">([^<]*)</div>"));
    }

    [Fact]
    public async Task AnUnknownUsernameLeadsBackToTheLoginPageWithTheMessage()
    {
        var client = Browser();

        var refused = await LogInAsync(client, "operator", "admin");

        Assert.Equal(HttpStatusCode.Redirect, refused.StatusCode);
        Assert.StartsWith("/backoffice/login", refused.Headers.Location!.ToString(), StringComparison.Ordinal);
        var page = await client.GetStringAsync(refused.Headers.Location);
        Assert.Equal("Invalid username or password.", Text(page, @"data-test=""login-error-message"">([^<]*)</div>"));
    }

    /// <summary>A browser of its own, with no cookies yet; redirects are not followed — each one is an assertion.</summary>
    private HttpClient Browser() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Fills in the login form as rendered, its antiforgery field included, and submits it.</summary>
    private static async Task<HttpResponseMessage> LogInAsync(HttpClient client, string username, string password)
    {
        var form = Element(await client.GetStringAsync("/backoffice/login"), "backoffice-login-form", "</form>");
        var hidden = Regex.Matches(form, @"<input\b[^>]*type=""hidden""[^>]*>")
            .Select(input => new KeyValuePair<string, string>(
                Regex.Match(input.Value, @"name=""([^""]+)""").Groups[1].Value,
                WebUtility.HtmlDecode(Regex.Match(input.Value, @"value=""([^""]*)""").Groups[1].Value)));

        return await client.PostAsync(
            "/backoffice/login",
            new FormUrlEncodedContent(hidden.Concat(
                [new KeyValuePair<string, string>("username", username), new KeyValuePair<string, string>("password", password)])));
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
        return Rendered(match.Groups[1].Value);
    }

    /// <summary>The text a reader sees: tags removed, entities decoded.</summary>
    private static string Rendered(string html) => WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", string.Empty)).Trim();
}