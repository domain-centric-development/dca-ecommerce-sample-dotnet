using DcaShop.Inventory.Application.ProvisionInitialStock;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A seeded product whose stock has run out, as the catalogue and its product page show it. The stock is taken to
/// 0 through Inventory's own input port; this class has a host of its own, so no other test sees the product gone.
/// </summary>
public sealed class OutOfStockProductTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SoldOut = "Wooden Hexagon Desk Model";

    private readonly WebApplicationFactory<Program> _factory;

    public OutOfStockProductTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TheCardOfAProductWithNoStockShowsOutOfStock()
    {
        var client = _factory.CreateClient();
        await NoStockLeftAsync(client);

        var catalogue = await client.GetStringAsync("/products");

        var card = Assert.Single(AvailabilityPages.Cards(catalogue), c => AvailabilityPages.CardName(c) == SoldOut);
        Assert.Equal("Out of Stock", AvailabilityPages.CardBadge(card));
    }

    [Fact]
    public async Task TheProductPageOfAProductWithNoStockShowsItOutOfStockAndOffersOnlyTheWayBack()
    {
        var client = _factory.CreateClient();
        var path = await NoStockLeftAsync(client);

        var page = await client.GetStringAsync(path);

        Assert.Equal("Out of Stock", AvailabilityPages.Badge(page));
        Assert.Equal("Currently out of stock", AvailabilityPages.Availability(page));
        var actions = AvailabilityPages.Actions(page);
        Assert.DoesNotContain("data-test=\"product-add-to-cart-button\"", actions, StringComparison.Ordinal);
        Assert.DoesNotContain("<button", actions, StringComparison.Ordinal);
        Assert.Equal("Back to Products", AvailabilityPages.Text(actions, @"data-test=""product-back-link"">([^<]*)</a>"));
    }

    /// <summary>Sets the stock of the sold-out product to 0 and answers its page's path.</summary>
    private async Task<string> NoStockLeftAsync(HttpClient client)
    {
        var path = await AvailabilityPages.ProductPathAsync(client, SoldOut);
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IProvisionInitialStockInputPort>()
            .ExecuteAsync(new ProvisionInitialStockCommand(Guid.Parse(path["/products/".Length..]), 0));
        return path;
    }
}