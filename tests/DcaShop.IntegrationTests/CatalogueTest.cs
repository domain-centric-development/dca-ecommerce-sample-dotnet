using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The catalogue page with the seeded sample catalogue, through the real HTTP pipeline: the order of its cards, what a
/// card shows, where a card leads and how the page names itself.
/// </summary>
public sealed class CatalogueTest : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Regex CardTitle = new(@"data-test=""product-card-title"">([^<]*)<", RegexOptions.Compiled);
    private static readonly Regex CardAction = new(@"<a\b[^>]*href=""([^""]+)""[^>]*data-test=""view-product"">([^<]*)</a>", RegexOptions.Compiled);

    private readonly WebApplicationFactory<Program> _factory;

    public CatalogueTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheCardsAreInOrdinalOrderOfTheProductNameStartingWithTheEnamelPin()
    {
        var client = _factory.CreateClient();

        var catalogue = await client.GetStringAsync("/products");

        var titles = Cards(catalogue).Select(card => Text(card, CardTitle.ToString())).ToList();
        Assert.Equal(titles.Order(StringComparer.Ordinal), titles);
        Assert.Equal("\"Bounded Context\" Enamel Pin", titles[0]);
    }

    [Fact]
    public async Task TheCardOfDomainDrivenDesignShowsItsNameDescriptionAndImage()
    {
        var client = _factory.CreateClient();

        var catalogue = await client.GetStringAsync("/products");

        var card = Cards(catalogue).Single(c => Text(c, CardTitle.ToString()) == "Domain-Driven Design");
        Assert.Equal(
            "The seminal work by Eric Evans that introduced the software industry to Domain-Driven Design. This essential "
            + "guide teaches you how to tackle complexity in the heart of software by connecting implementation to an "
            + "evolving model of the business domain.",
            Text(card, @"class=""product-card__description"">([^<]*)</p>"));
        var image = Regex.Match(card, @"<img\s+src=""([^""]+)""\s+alt=""([^""]*)""");
        Assert.True(image.Success, "the card shows an image");
        Assert.Equal("/images/products/ddd-book.webp", image.Groups[1].Value);
        Assert.Equal("Domain-Driven Design", WebUtility.HtmlDecode(image.Groups[2].Value));
        var served = await client.GetAsync(image.Groups[1].Value);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.StartsWith("image/", served.Content.Headers.ContentType?.MediaType, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EveryCardOffersViewDetailsLeadingToItsOwnProduct()
    {
        var client = _factory.CreateClient();

        var catalogue = await client.GetStringAsync("/products");

        var cards = Cards(catalogue);
        Assert.NotEmpty(cards);
        foreach (var card in cards)
        {
            var name = Text(card, CardTitle.ToString());
            var action = CardAction.Match(card);
            Assert.True(action.Success, $"the card of {name} offers a link");
            Assert.Equal("View Details", WebUtility.HtmlDecode(action.Groups[2].Value).Trim());
            var productPage = await client.GetStringAsync(action.Groups[1].Value);
            Assert.Equal(name, Text(productPage, @"data-test=""product-detail-title"">([^<]*)</h1>"));
        }
    }

    [Fact]
    public async Task TheCatalogueIsTitledProductCatalogWithHeadingOurProductsBelowHomeProducts()
    {
        var client = _factory.CreateClient();

        var catalogue = await client.GetStringAsync("/products");

        Assert.Equal("Product Catalog", Text(catalogue, @"<title>([^<]*)</title>"));
        Assert.Equal("Our Products", Text(catalogue, @"<h1>([^<]*)</h1>"));
        var breadcrumb = Section(catalogue, "breadcrumb").Split("</div>")[0];
        var parts = Regex.Matches(breadcrumb, @"<(a|span)\b[^>]*>([^<]*)</\1>")
            .Select(m => WebUtility.HtmlDecode(m.Groups[2].Value).Trim());
        Assert.Equal("Home / Products", string.Join(" ", parts));
    }

    /// <summary>The markup of each catalogue card, in page order.</summary>
    private static List<string> Cards(string catalogue) =>
        catalogue.Split("data-test=\"product-card\"").Skip(1).ToList();

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
}