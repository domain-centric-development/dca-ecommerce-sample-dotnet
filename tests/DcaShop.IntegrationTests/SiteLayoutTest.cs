using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The header and footer every page shares, through the real HTTP pipeline: where the header navigation leads, what
/// the footer says, and that the home page carries the same frame as the catalogue.
/// </summary>
public sealed class SiteLayoutTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SiteLayoutTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HomeInTheHeaderNavigationOpensTheHomePage()
    {
        var client = _factory.CreateClient();
        var catalogue = await client.GetStringAsync("/products");
        var home = Link(Header(catalogue), "nav-home-link");
        Assert.Equal("Home", home.Text);

        var followed = await client.GetStringAsync(home.Target);

        Assert.Equal("Welcome to domaincentric.commerce", Rendered(Match(followed, @"<h1[^>]*>(.*?)</h1>")));
    }

    [Fact]
    public async Task ProductsInTheHeaderNavigationOpensTheCatalogue()
    {
        var client = _factory.CreateClient();
        var homePage = await client.GetStringAsync("/");
        var products = Link(Header(homePage), "nav-products-link");
        Assert.Equal("Products", products.Text);

        var followed = await client.GetStringAsync(products.Target);

        Assert.Equal("Our Products", Rendered(Match(followed, @"<h1[^>]*>(.*?)</h1>")));
    }

    [Fact]
    public async Task TheFooterNamesTheShopAndLinksToTheEventLog()
    {
        var client = _factory.CreateClient();

        var catalogue = await client.GetStringAsync("/products");

        var footer = Footer(catalogue);
        Assert.Equal(
            "domaincentric.commerce — Built with Domain-Centric Architecture",
            Rendered(Match(footer, @"class=""site-footer__text"">(.*?)</div>")));
        var eventLog = Link(footer, "footer-event-log-link");
        Assert.Equal("Event Log", eventLog.Text);
        Assert.Equal("/backoffice/events", eventLog.Target);
    }

    [Fact]
    public async Task TheHomePageShowsTheSameHeaderAndFooterAsTheCatalogue()
    {
        var client = _factory.CreateClient();
        var catalogue = await client.GetStringAsync("/products");

        var homePage = await client.GetStringAsync("/");

        Assert.Equal("Home", Link(Header(homePage), "nav-home-link").Text);
        Assert.Equal("Products", Link(Header(homePage), "nav-products-link").Text);
        Assert.Equal(Header(catalogue), Header(homePage));
        Assert.Equal(Footer(catalogue), Footer(homePage));
    }

    private static string Header(string page) => Element(page, "site-header", "</header>");

    private static string Footer(string page) => Element(page, "site-footer", "</footer>");

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