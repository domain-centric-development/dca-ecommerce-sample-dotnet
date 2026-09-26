using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The product page of a seeded product, through the real HTTP pipeline: what it shows about the product, how it
/// names itself and where it leads back to. The product is found the way a visitor finds it, on its catalogue card.
/// </summary>
public sealed class ProductPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Regex CardLink = new(@"href=""(/products/[0-9a-f-]{36})""\s+data-test=""view-product""", RegexOptions.Compiled);
    private static readonly Regex CardTitle = new(@"data-test=""product-card-title"">([^<]*)<", RegexOptions.Compiled);

    private readonly WebApplicationFactory<Program> _factory;

    public ProductPageTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheProductPageShowsTheImageTheDescriptionAndTheCategory()
    {
        var client = _factory.CreateClient();

        var page = await client.GetStringAsync(await ProductPathAsync(client, "Domain-Driven Design"));

        var detail = Section(page, "product-detail");
        var image = Regex.Match(detail, @"<img\s+src=""([^""]+)""\s+alt=""([^""]*)""");
        Assert.True(image.Success, "the page shows an image");
        Assert.Equal("/images/products/ddd-book.webp", image.Groups[1].Value);
        Assert.Equal("Domain-Driven Design", WebUtility.HtmlDecode(image.Groups[2].Value));
        var served = await client.GetAsync(image.Groups[1].Value);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.StartsWith("image/", served.Content.Headers.ContentType?.MediaType, StringComparison.Ordinal);
        Assert.Equal(
            "The seminal work by Eric Evans that introduced the software industry to Domain-Driven Design. This essential "
            + "guide teaches you how to tackle complexity in the heart of software by connecting implementation to an "
            + "evolving model of the business domain.",
            Text(detail, @"class=""product-detail__description""><p>([^<]*)</p>"));
        Assert.Equal("Books", MetaValue(detail, "Category"));
    }

    [Fact]
    public async Task TheBrowserTabTitleIsTheProductName()
    {
        var client = _factory.CreateClient();

        var page = await client.GetStringAsync(await ProductPathAsync(client, "Clean Architecture"));

        Assert.Equal("Clean Architecture", Text(page, @"<title>([^<]*)</title>"));
    }

    [Fact]
    public async Task TheBreadcrumbLeadsFromHomeOverProductsToTheProduct()
    {
        var client = _factory.CreateClient();

        var page = await client.GetStringAsync(await ProductPathAsync(client, "Clean Architecture"));

        var breadcrumb = Section(page, "breadcrumb").Split("</div>")[0];
        var parts = Regex.Matches(breadcrumb, @"<(a|span)\b[^>]*>([^<]*)</\1>")
            .Select(m => WebUtility.HtmlDecode(m.Groups[2].Value).Trim());
        Assert.Equal("Home / Products / Clean Architecture", string.Join(" ", parts));
        Assert.Equal("/", LinkTarget(breadcrumb, "Home"));
        Assert.Equal("/products", LinkTarget(breadcrumb, "Products"));
        Assert.Contains("<h1>Our Products</h1>", await client.GetStringAsync(LinkTarget(breadcrumb, "Products")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task BackToProductsReturnsToTheCatalogue()
    {
        var client = _factory.CreateClient();
        var page = await client.GetStringAsync(await ProductPathAsync(client, "Team Topologies"));
        var back = Regex.Match(page, @"<a\b[^>]*href=""([^""]+)""[^>]*data-test=""product-back-link"">([^<]*)</a>");
        Assert.True(back.Success, "the page offers a way back");
        Assert.Equal("Back to Products", back.Groups[2].Value);

        var followed = await client.GetStringAsync(back.Groups[1].Value);

        Assert.Contains("data-test=\"product-grid\"", followed, StringComparison.Ordinal);
        Assert.Equal("Our Products", Text(followed, @"<h1>([^<]*)</h1>"));
    }

    /// <summary>The "View Details" target of the catalogue card titled <paramref name="name"/>.</summary>
    private static async Task<string> ProductPathAsync(HttpClient client, string name)
    {
        var catalogue = await client.GetStringAsync("/products");
        var card = catalogue.Split("data-test=\"product-card\"")
            .Skip(1)
            .Single(c => CardTitle.Match(c) is { Success: true } t && WebUtility.HtmlDecode(t.Groups[1].Value).Trim() == name);
        return CardLink.Match(card).Groups[1].Value;
    }

    /// <summary>The markup from the element carrying <paramref name="dataTest"/> to the end of the page.</summary>
    private static string Section(string page, string dataTest)
    {
        var start = page.IndexOf($"data-test=\"{dataTest}\"", StringComparison.Ordinal);
        Assert.True(start >= 0, $"the page has a {dataTest}");
        return page[start..];
    }

    private static string Text(string markup, string pattern)
    {
        var match = Regex.Match(markup, pattern);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
    }

    private static string MetaValue(string detail, string label) =>
        Text(detail, $@"class=""product-detail__meta-label"">{Regex.Escape(label)}</span><span class=""product-detail__meta-value"">([^<]*)</span>");

    private static string LinkTarget(string markup, string text) =>
        Text(markup, $@"<a\b[^>]*href=""([^""]+)""[^>]*>{Regex.Escape(text)}</a>");
}