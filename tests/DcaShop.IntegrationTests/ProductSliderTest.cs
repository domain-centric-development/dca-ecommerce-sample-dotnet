using System.Text.RegularExpressions;

using DcaShop.Product.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The homepage's "Discover products" slider over a catalogue whose prices the test decides: the seeded products,
/// with Pricing's answer replaced by one that prices only the products a test names — the state of a product nobody
/// has priced yet. What the browser suite cannot arrange, because it runs against the seeded shop.
/// </summary>
public sealed class ProductSliderTest : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Regex CardLink = new(@"<a\b[^>]*\bdata-test=""product-slider-card-link""[^>]*>", RegexOptions.Compiled);
    private static readonly Regex Href = new(@"\bhref=""/products/([0-9a-f-]{36})""", RegexOptions.Compiled);

    private readonly WebApplicationFactory<Program> _factory;

    public ProductSliderTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AProductWithoutAPriceIsNotAmongTheSlidersCards()
    {
        var pricing = new ChosenPricing();
        var client = ShopWith(pricing, out var services).CreateClient();
        var catalogue = await CatalogueAsync(services);
        var priced = catalogue.Take(10).ToHashSet();
        pricing.PriceOnly(priced);

        for (var request = 0; request < 10; request++)
        {
            var cards = CardProductIds(await client.GetStringAsync("/"));

            Assert.Equal(8, cards.Count);
            Assert.All(cards, id => Assert.Contains(id, priced));
        }
    }

    [Fact]
    public async Task WithTwoPricedProductsTheSliderHoldsACardForEachOfThem()
    {
        var pricing = new ChosenPricing();
        var client = ShopWith(pricing, out var services).CreateClient();
        var catalogue = await CatalogueAsync(services);
        var priced = new[] { catalogue[3], catalogue[17] }.ToHashSet();
        pricing.PriceOnly(priced);

        var cards = CardProductIds(await client.GetStringAsync("/"));

        Assert.Equal(2, cards.Count);
        Assert.Equal(priced, cards.ToHashSet());
    }

    [Fact]
    public async Task WithoutAnyPricedProductTheHomepageShowsNoSlider()
    {
        var pricing = new ChosenPricing();
        var client = ShopWith(pricing, out _).CreateClient();

        var home = await client.GetStringAsync("/");

        Assert.Contains("data-test=\"hero\"", home, StringComparison.Ordinal);
        Assert.DoesNotContain("data-test=\"product-slider\"", home, StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> ShopWith(ChosenPricing pricing, out IServiceProvider services)
    {
        var shop = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(s => s.AddSingleton<IPricingDataPort>(pricing)));
        services = shop.Services;
        return shop;
    }

    private static async Task<IReadOnlyList<Guid>> CatalogueAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var products = await scope.ServiceProvider.GetRequiredService<IProductRepository>().FindAllAsync();
        Assert.Equal(21, products.Count);
        return products.Select(p => p.Id.Value).ToList();
    }

    /// <summary>The product of every slider card, read from the card's link to its product page.</summary>
    private static IReadOnlyList<Guid> CardProductIds(string html) =>
        CardLink.Matches(html)
            .Select(tag => Href.Match(tag.Value))
            .Where(href => href.Success)
            .Select(href => Guid.Parse(href.Groups[1].Value))
            .ToList();

    /// <summary>Pricing's answer holds only the products priced here; every other product has no price.</summary>
    private sealed class ChosenPricing : IPricingDataPort
    {
        private volatile IReadOnlySet<Guid> _priced = new HashSet<Guid>();

        public void PriceOnly(IReadOnlySet<Guid> products) => _priced = products;

        public Task<IReadOnlyDictionary<ProductId, PriceData>> GetPricesAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, PriceData>>(
                productIds.Where(id => _priced.Contains(id.Value)).ToDictionary(id => id, id => new PriceData(id, Money.Euro(10m))));
    }
}