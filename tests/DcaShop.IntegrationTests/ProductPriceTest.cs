using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The price of a seeded product on its catalogue card and on its product page, through the real HTTP pipeline: the
/// price Pricing set from the product's creation, written as its amount with two decimals and the currency code.
/// </summary>
public sealed class ProductPriceTest : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Regex CardTitle = new(@"data-test=""product-card-title"">([^<]*)<", RegexOptions.Compiled);
    private static readonly Regex CardLink = new(@"href=""(/products/[0-9a-f-]{36})""\s+data-test=""view-product""", RegexOptions.Compiled);
    private static readonly Regex CardPrice = new(@"class=""product-card__price"">([^<]*)<", RegexOptions.Compiled);
    private static readonly Regex PagePrice = new(
        @"data-test=""product-detail-price"">([^<]*)</span><span class=""product-detail__price-label"">([^<]*)</span>",
        RegexOptions.Compiled);

    /// <summary>The sample catalogue's prices, as the shop seeds them.</summary>
    private static readonly IReadOnlyDictionary<string, string> SeededPrices = new Dictionary<string, string>
    {
        ["Domain-Driven Design"] = "54.99 EUR",
        ["Clean Architecture"] = "39.99 EUR",
        ["Implementing Domain-Driven Design"] = "49.99 EUR",
        ["Domain-Driven Design Distilled"] = "24.99 EUR",
        ["Hexagonal Architecture Explained"] = "34.99 EUR",
        ["Learning Domain-Driven Design"] = "44.99 EUR",
        ["Patterns of Enterprise Application Architecture"] = "59.99 EUR",
        ["Team Topologies"] = "29.99 EUR",
        ["Event Storming Sticky Note Kit"] = "34.99 EUR",
        ["Hexagon Whiteboard Magnets, Set of 24"] = "24.99 EUR",
        ["Context Map Poster, A1"] = "19.99 EUR",
        ["DDD Pattern Card Deck"] = "22.99 EUR",
        ["\"Ports & Adapters\" T-Shirt"] = "29.99 EUR",
        ["\"Domain over Framework\" Hoodie"] = "59.99 EUR",
        ["Hexagon Embroidered Cap"] = "24.99 EUR",
        ["\"Ubiquitous Language\" Mug"] = "16.99 EUR",
        ["Hexagon Wooden Coasters, Set of 6"] = "27.99 EUR",
        ["Hex-Grid Modeling Notebook"] = "18.99 EUR",
        ["Wooden Hexagon Desk Model"] = "39.99 EUR",
        ["Hexagon Sticker Sheet"] = "9.99 EUR",
        ["\"Bounded Context\" Enamel Pin"] = "12.99 EUR",
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ProductPriceTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheProductPageShowsThePriceLabelledRetailPrice()
    {
        var client = _factory.CreateClient();

        var page = await client.GetStringAsync(CardLink.Match(await CardAsync(client, "Domain-Driven Design")).Groups[1].Value);

        var price = PagePrice.Match(page);
        Assert.True(price.Success, "the product page shows a labelled price");
        Assert.Equal("54.99 EUR", WebUtility.HtmlDecode(price.Groups[1].Value).Trim());
        Assert.Equal("Retail Price", WebUtility.HtmlDecode(price.Groups[2].Value).Trim());
    }

    [Fact]
    public async Task APriceBelowTenKeepsBothDecimalsFollowedByTheCurrency()
    {
        var client = _factory.CreateClient();

        var card = await CardAsync(client, "Hexagon Sticker Sheet");

        Assert.Equal("9.99 EUR", Text(card, CardPrice));
    }

    [Fact]
    public async Task EveryCardOfTheSeededCatalogueShowsTheSeededPrice()
    {
        var client = _factory.CreateClient();

        var catalogue = await client.GetStringAsync("/products");

        var shown = Cards(catalogue).ToDictionary(card => Text(card, CardTitle), card => Text(card, CardPrice));
        Assert.Equal(21, shown.Count);
        Assert.Equal(SeededPrices.OrderBy(p => p.Key, StringComparer.Ordinal), shown.OrderBy(p => p.Key, StringComparer.Ordinal));
        Assert.Equal("39.99 EUR", shown["Wooden Hexagon Desk Model"]);
        Assert.Equal("12.99 EUR", shown["\"Bounded Context\" Enamel Pin"]);
    }

    [Fact]
    public async Task TheProductPageShowsTheSamePriceAsTheCard()
    {
        var client = _factory.CreateClient();
        var card = await CardAsync(client, "Patterns of Enterprise Application Architecture");
        Assert.Equal("59.99 EUR", Text(card, CardPrice));

        var page = await client.GetStringAsync(CardLink.Match(card).Groups[1].Value);

        Assert.Equal("59.99 EUR", Text(page, PagePrice));
    }

    /// <summary>The markup of the catalogue card titled <paramref name="name"/>.</summary>
    private static async Task<string> CardAsync(HttpClient client, string name) =>
        Cards(await client.GetStringAsync("/products")).Single(card => Text(card, CardTitle) == name);

    /// <summary>The markup of each catalogue card, in page order.</summary>
    private static List<string> Cards(string catalogue) =>
        catalogue.Split("data-test=\"product-card\"").Skip(1).ToList();

    private static string Text(string markup, Regex pattern)
    {
        var match = pattern.Match(markup);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
    }
}