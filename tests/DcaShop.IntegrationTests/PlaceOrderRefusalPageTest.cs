using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A "Place Order" for a product another visitor has since sold out is refused: the review again, at its own address,
/// with the error. A host of its own: the Hexagon is sold out here.
/// </summary>
public sealed class PlaceOrderRefusalPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    private readonly WebApplicationFactory<Program> _factory;

    public PlaceOrderRefusalPageTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ARefusedPlaceOrderOpensTheReviewAgainWithItsError()
    {
        var guest = GuestOrders.Guest(_factory);
        var hexagon = await AvailabilityPages.ProductPathAsync(guest, Hexagon);
        await GuestOrders.GoToTheReviewAsync(guest, (hexagon, 2));
        await GuestOrders.AnotherGuestHasBoughtAsync(_factory, Guid.Parse(hexagon["/products/".Length..]), 8);

        var placed = await guest.PostAsync("/checkout/confirm", GuestOrders.Form(await guest.GetStringAsync("/checkout/review"), ("placedFrom", "review")));

        Assert.Equal(HttpStatusCode.Redirect, placed.StatusCode);
        Assert.Equal("/checkout/review", placed.Headers.Location!.ToString());
        var review = await guest.GetStringAsync(placed.Headers.Location);
        Assert.Equal(
            "Cannot place the order, 1 item(s) unavailable or out of stock.",
            AvailabilityPages.Text(review, @"data-test=""review-error-message"">([^<]*)</div>"));
        Assert.DoesNotContain("data-test=\"confirmation-title\"", review, StringComparison.Ordinal);
    }
}