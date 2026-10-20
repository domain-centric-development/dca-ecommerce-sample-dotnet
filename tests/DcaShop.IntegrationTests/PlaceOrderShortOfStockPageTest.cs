using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A "Place Order" for more of a product than another visitor's order has since left is refused: the review again, at
/// its own address, naming the product — and neither the stock nor the cart changes. A host of its own: the Hexagon's
/// stock is used up here.
/// </summary>
public sealed class PlaceOrderShortOfStockPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    private readonly WebApplicationFactory<Program> _factory;

    public PlaceOrderShortOfStockPageTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task APlaceOrderForMoreThanTheStockLeftNamesTheProductAndChangesNothing()
    {
        var guest = GuestOrders.Guest(_factory);
        var hexagon = await AvailabilityPages.ProductPathAsync(guest, Hexagon);
        await GuestOrders.GoToTheReviewAsync(guest, (hexagon, 5));
        await GuestOrders.AnotherGuestHasBoughtAsync(_factory, Guid.Parse(hexagon["/products/".Length..]), 5);

        var placed = await guest.PostAsync("/checkout/confirm", GuestOrders.Form(await guest.GetStringAsync("/checkout/review"), ("placedFrom", "review")));

        Assert.Equal(HttpStatusCode.Redirect, placed.StatusCode);
        Assert.Equal("/checkout/review", placed.Headers.Location!.ToString());
        var review = await guest.GetStringAsync(placed.Headers.Location);
        Assert.Equal(
            "Not enough stock left for 'Wooden Hexagon Desk Model'. Please adjust your order.",
            AvailabilityPages.Text(review, @"data-test=""review-error-message"">([^<]*)</div>"));
        Assert.DoesNotContain("data-test=\"confirmation-title\"", review, StringComparison.Ordinal);
        Assert.Equal("3 units available", await GuestOrders.AvailabilityAsync(guest, hexagon));
        Assert.Equal([$"{Hexagon} x5"], CartPages.Lines(await guest.GetStringAsync("/cart")));
    }
}