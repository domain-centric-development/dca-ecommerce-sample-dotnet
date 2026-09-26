using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The page the shop answers for a product address no product has, through the real HTTP pipeline: where its two
/// ways on lead.
/// </summary>
public sealed class NotFoundPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string UnknownProduct = "/products/no-such-product";

    private readonly WebApplicationFactory<Program> _factory;

    public NotFoundPageTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BrowseAllProductsOnTheNotFoundPageOpensTheCatalogue()
    {
        var client = _factory.CreateClient();
        var notFound = await NotFoundPage(client);
        var browse = Link(notFound, "error-browse-link");
        Assert.Equal("Browse All Products", browse.Text);

        var followed = await client.GetStringAsync(browse.Target);

        Assert.Equal("Our Products", Rendered(Match(followed, @"<h1[^>]*>(.*?)</h1>")));
    }

    [Fact]
    public async Task GoToHomepageOnTheNotFoundPageOpensTheHomePage()
    {
        var client = _factory.CreateClient();
        var notFound = await NotFoundPage(client);
        var home = Link(notFound, "error-home-link");
        Assert.Equal("Go to Homepage", home.Text);

        var followed = await client.GetStringAsync(home.Target);

        Assert.Equal("Welcome to domaincentric.commerce", Rendered(Match(followed, @"<h1[^>]*>(.*?)</h1>")));
    }

    private static async Task<string> NotFoundPage(HttpClient client)
    {
        var response = await client.GetAsync(UnknownProduct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>The target and text of the anchor carrying <paramref name="dataTest"/>.</summary>
    private static (string Target, string Text) Link(string markup, string dataTest)
    {
        var match = Regex.Match(markup, $@"<a\b[^>]*href=""([^""]+)""[^>]*data-test=""{Regex.Escape(dataTest)}"">([^<]*)</a>");
        Assert.True(match.Success, $"the markup offers a {dataTest}");
        return (match.Groups[1].Value, Rendered(match.Groups[2].Value));
    }

    private static string Match(string markup, string pattern)
    {
        var match = Regex.Match(markup, pattern, RegexOptions.Singleline);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return match.Groups[1].Value;
    }

    /// <summary>The text a reader sees: tags removed, entities decoded.</summary>
    private static string Rendered(string html) => WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", string.Empty)).Trim();
}