using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// What the catalogue and the product page say about the stock of the seeded products, through the real HTTP
/// pipeline: the badge, the units left and whether the page offers to add the product to the cart.
/// </summary>
public sealed class ProductAvailabilityTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductAvailabilityTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AProductWithFewUnitsShowsExactlyHowManyAreLeft()
    {
        var client = _factory.CreateClient();

        var page = await client.GetStringAsync(await AvailabilityPages.ProductPathAsync(client, "Wooden Hexagon Desk Model"));

        Assert.Equal("8 units available", AvailabilityPages.Availability(page));
    }

    [Fact]
    public async Task EveryCardOfTheSeededCatalogueShowsInStock()
    {
        var client = _factory.CreateClient();

        var catalogue = await client.GetStringAsync("/products");

        var badges = AvailabilityPages.Cards(catalogue).Select(AvailabilityPages.CardBadge).ToList();
        Assert.Equal(21, badges.Count);
        Assert.All(badges, badge => Assert.Equal("In Stock", badge));
    }

    [Fact]
    public async Task AnAvailableProductOffersAddToCartNextToBackToProducts()
    {
        var client = _factory.CreateClient();

        var page = await client.GetStringAsync(await AvailabilityPages.ProductPathAsync(client, "Clean Architecture"));

        var actions = AvailabilityPages.Actions(page);
        Assert.Equal("Add to Cart", AvailabilityPages.Text(actions, @"data-test=""product-add-to-cart-button"">([^<]*)</button>"));
        Assert.Equal("Back to Products", AvailabilityPages.Text(actions, @"data-test=""product-back-link"">([^<]*)</a>"));
    }
}

/// <summary>The catalogue and product-page markup the availability tests read.</summary>
internal static class AvailabilityPages
{
    private static readonly Regex CardLink = new(@"href=""(/products/[0-9a-f-]{36})""\s+data-test=""view-product""", RegexOptions.Compiled);
    private static readonly Regex CardTitle = new(@"data-test=""product-card-title"">([^<]*)<", RegexOptions.Compiled);

    /// <summary>The markup of each product card, in page order.</summary>
    public static IReadOnlyList<string> Cards(string catalogue) =>
        catalogue.Split("data-test=\"product-card\"").Skip(1).ToList();

    public static string CardName(string card) => WebUtility.HtmlDecode(CardTitle.Match(card).Groups[1].Value).Trim();

    public static string CardBadge(string card) => Text(card, @"class=""product-card__badge\b[^""]*"">([^<]*)</span>");

    /// <summary>The "View Details" target of the catalogue card titled <paramref name="name"/>.</summary>
    public static async Task<string> ProductPathAsync(HttpClient client, string name)
    {
        var catalogue = await client.GetStringAsync("/products");
        return CardLink.Match(Cards(catalogue).Single(card => CardName(card) == name)).Groups[1].Value;
    }

    /// <summary>The badge in the product page's header.</summary>
    public static string Badge(string page) =>
        Text(Section(page, "product-detail"), @"<span class=""badge\b[^""]*"">([^<]*)</span>");

    /// <summary>The value the product page shows beside the label "Availability".</summary>
    public static string Availability(string page) =>
        Text(Section(page, "product-detail"), @"class=""product-detail__meta-label"">Availability</span>\s*<span class=""product-detail__meta-value\b[^""]*"">([^<]*)</span>");

    /// <summary>The product page's action bar, from its opening tag to its end.</summary>
    public static string Actions(string page)
    {
        var start = page.IndexOf("class=\"product-detail__actions\"", StringComparison.Ordinal);
        Assert.True(start >= 0, "the page has an action bar");
        var actions = page[start..];
        return actions[..actions.IndexOf("</div>", StringComparison.Ordinal)];
    }

    /// <summary>The markup from the element carrying <paramref name="dataTest"/> to the end of the page.</summary>
    public static string Section(string page, string dataTest)
    {
        var start = page.IndexOf($"data-test=\"{dataTest}\"", StringComparison.Ordinal);
        Assert.True(start >= 0, $"the page has a {dataTest}");
        return page[start..];
    }

    public static string Text(string markup, string pattern)
    {
        var match = Regex.Match(markup, pattern);
        Assert.True(match.Success, $"the markup matches {pattern}");
        return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
    }
}