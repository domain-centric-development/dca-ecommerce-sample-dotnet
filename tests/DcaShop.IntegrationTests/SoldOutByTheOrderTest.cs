using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The last units of a product bought by one order sell it out. A host of its own: the product is gone afterwards.
/// </summary>
public sealed class SoldOutByTheOrderTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    private readonly WebApplicationFactory<Program> _factory;

    public SoldOutByTheOrderTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BuyingTheLastUnitsShowsTheProductOutOfStockOnItsPageAndItsCard()
    {
        var guest = GuestOrders.Guest(_factory);
        var hexagon = await AvailabilityPages.ProductPathAsync(guest, Hexagon);
        Assert.Equal("8 units available", await GuestOrders.AvailabilityAsync(guest, hexagon));

        await GuestOrders.PlaceOrderAsync(guest, (hexagon, 8));

        Assert.Equal("Currently out of stock", await GuestOrders.AvailabilityShortlyAfterwardsAsync(guest, hexagon, "Currently out of stock"));
        var actions = AvailabilityPages.Actions(await guest.GetStringAsync(hexagon));
        Assert.DoesNotContain("data-test=\"product-add-to-cart-button\"", actions, StringComparison.Ordinal);
        var card = Assert.Single(AvailabilityPages.Cards(await guest.GetStringAsync("/products")), c => AvailabilityPages.CardName(c) == Hexagon);
        Assert.Equal("Out of Stock", AvailabilityPages.CardBadge(card));
    }
}