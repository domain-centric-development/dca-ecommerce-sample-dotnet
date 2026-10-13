using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Who the back office lets in, through the real HTTP pipeline: a shop customer's session is not an operator's, an
/// address below the back office leads through the login to the event log, and a logout closes it again and says so.
/// Each test is a browser of its own that keeps its cookies; redirects are not followed — each one is an assertion.
/// </summary>
public sealed class BackofficeAccessTest : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Uri Origin = new("http://localhost");

    private readonly WebApplicationFactory<Program> _factory;

    public BackofficeAccessTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ACustomerLoggedInToTheShopIsSentToTheBackofficeLogin()
    {
        var client = Browser();
        var registered = await RegisterAsync(client, "anna.schmidt@example.com");
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);

        var response = await client.GetAsync("/backoffice/events");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/backoffice/login", PathOf(response));
    }

    [Fact]
    public async Task LoggingInAfterBeingSentFromAnUnknownAddressOpensTheEventLog()
    {
        var client = Browser();
        var sent = await client.GetAsync("/backoffice/settings");
        Assert.Equal(HttpStatusCode.Redirect, sent.StatusCode);
        Assert.Equal("/backoffice/login", PathOf(sent));

        var loggedIn = await LogInAsync(client, "admin", "admin");

        Assert.Equal(HttpStatusCode.Redirect, loggedIn.StatusCode);
        Assert.Equal("/backoffice/events", PathOf(loggedIn));
        var page = await client.GetStringAsync(loggedIn.Headers.Location);
        Assert.Equal("Event Publication Log", Text(page, @"<h1>([^<]*)</h1>"));
    }

    [Fact]
    public async Task LoggingOutOpensTheLoginPageWithTheConfirmation()
    {
        var client = await SignedInAsync();

        var loggedOut = await LogOutAsync(client);

        Assert.Equal(HttpStatusCode.Redirect, loggedOut.StatusCode);
        Assert.Equal("/backoffice/login?logout=true", loggedOut.Headers.Location!.ToString());
        var page = await client.GetStringAsync(loggedOut.Headers.Location);
        Assert.Equal("You have been logged out.", Text(page, @"data-test=""logout-message"">([^<]*)</div>"));
    }

    [Fact]
    public async Task AfterTheLogoutTheEventLogSendsTheOperatorToTheLoginAgain()
    {
        var client = await SignedInAsync();
        Assert.Equal(HttpStatusCode.Redirect, (await LogOutAsync(client)).StatusCode);

        var response = await client.GetAsync("/backoffice/events");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/backoffice/login", PathOf(response));
    }

    /// <summary>A browser of its own, with no cookies yet; it keeps the ones it is given and follows no redirect.</summary>
    private HttpClient Browser() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>A browser signed in as the operator, standing on the event log.</summary>
    private async Task<HttpClient> SignedInAsync()
    {
        var client = Browser();
        var login = await LogInAsync(client, "admin", "admin");
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/backoffice/events", login.Headers.Location!.ToString());
        return client;
    }

    /// <summary>Fills in the login form as rendered, its antiforgery field included, and submits it.</summary>
    private static async Task<HttpResponseMessage> LogInAsync(HttpClient client, string username, string password)
    {
        var form = Element(await client.GetStringAsync("/backoffice/login"), "backoffice-login-form", "</form>");
        return await client.PostAsync(
            "/backoffice/login", Form(form, ("username", username), ("password", password)));
    }

    /// <summary>Presses "Logout" on the event log: submits its logout form as rendered.</summary>
    private static async Task<HttpResponseMessage> LogOutAsync(HttpClient client)
    {
        var form = Element(await client.GetStringAsync("/backoffice/events"), "event-log-logout-form", "</form>");
        return await client.PostAsync("/backoffice/logout", Form(form));
    }

    /// <summary>Registers a shop account, which logs the browser in to the shop.</summary>
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
                ("firstName", "Anna"),
                ("lastName", "Schmidt"),
                ("dateOfBirth", "1990-05-17")));
    }

    /// <summary>The fields given plus the antiforgery token found in <paramref name="markup"/>.</summary>
    private static FormUrlEncodedContent Form(string markup, params (string Name, string Value)[] fields)
    {
        var token = Regex
            .Match(markup, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""")
            .Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token), "the form carries an antiforgery token");

        return new FormUrlEncodedContent(
            fields.Append(("__RequestVerificationToken", token))
                .Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }

    /// <summary>The path a redirect leads to, whether the location is written absolute or relative.</summary>
    private static string PathOf(HttpResponseMessage response) =>
        new Uri(Origin, response.Headers.Location!).AbsolutePath;

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