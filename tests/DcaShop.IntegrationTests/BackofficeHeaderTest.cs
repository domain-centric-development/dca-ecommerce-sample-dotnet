using System.Net;
using System.Text.RegularExpressions;

using DcaShop.Backoffice.Application.GetEventPublications;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The header the back office's pages carry, through the real HTTP pipeline: a browser logged in to the shop whose
/// operator then logs in to the back office sees nothing of the shopper there. The event log's input port answers an
/// empty log; the header does not depend on it.
/// </summary>
public sealed class BackofficeHeaderTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Email = "anna.schmidt@example.com";

    private readonly WebApplicationFactory<Program> _factory;

    public BackofficeHeaderTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheEventLogsHeaderShowsNothingOfTheShopperLoggedInToTheShop()
    {
        var client = Browser();
        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, Email)).StatusCode);
        Assert.Equal($"Welcome, {Email}", Text(Header(await client.GetStringAsync("/products")), "user-greeting"));
        Assert.Equal(HttpStatusCode.Redirect, (await LogInAsync(client, "admin", "admin")).StatusCode);

        var page = await client.GetStringAsync("/backoffice/events");

        Assert.Contains("<h1>Event Publication Log</h1>", page, StringComparison.Ordinal);
        var header = Header(page);
        Assert.DoesNotContain($"Welcome, {Email}", Rendered(header), StringComparison.Ordinal);
        Assert.DoesNotContain("data-test=\"user-greeting\"", header, StringComparison.Ordinal);
        Assert.DoesNotContain("My Account", Rendered(header), StringComparison.Ordinal);
        Assert.DoesNotContain("data-test=\"nav-my-account-link\"", header, StringComparison.Ordinal);
        Assert.DoesNotContain("data-test=\"mini-basket\"", header, StringComparison.Ordinal);
    }

    /// <summary>A browser of its own over an empty event log; it keeps its cookies and follows no redirect.</summary>
    private HttpClient Browser() =>
        _factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddSingleton<IGetEventPublicationsInputPort>(new EmptyLog())))
            .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>Fills in the back office's login form as rendered, its antiforgery field included, and submits it.</summary>
    private static async Task<HttpResponseMessage> LogInAsync(HttpClient client, string username, string password)
    {
        var form = await client.GetStringAsync("/backoffice/login");
        return await client.PostAsync("/backoffice/login", Form(form, ("username", username), ("password", password)));
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

    /// <summary>The markup of the site header.</summary>
    private static string Header(string page)
    {
        var start = page.IndexOf("data-test=\"site-header\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "the page has a site header");
        return page[start..].Split("</header>")[0];
    }

    /// <summary>The text of the first element carrying <paramref name="dataTest"/>.</summary>
    private static string Text(string markup, string dataTest)
    {
        var match = Regex.Match(markup, $@"data-test=""{Regex.Escape(dataTest)}"">([^<]*)<");
        Assert.True(match.Success, $"the markup has a {dataTest}");
        return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
    }

    /// <summary>The text a reader sees: tags removed, entities decoded.</summary>
    private static string Rendered(string html) => WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", " "));

    /// <summary>The event log's input port, answering a log that holds nothing.</summary>
    private sealed class EmptyLog : IGetEventPublicationsInputPort
    {
        public Task<GetEventPublicationsResult> ExecuteAsync(
            GetEventPublicationsQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(GetEventPublicationsResult.From([]));
    }
}