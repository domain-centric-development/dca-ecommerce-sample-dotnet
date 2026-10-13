using System.Net;

using DcaShop.Inventory.Application.ReduceStock;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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
        await AnotherVisitorHasBoughtAsync(hexagon, 8);

        var placed = await guest.PostAsync("/checkout/confirm", GuestOrders.Form(await guest.GetStringAsync("/checkout/review")));

        Assert.Equal(HttpStatusCode.Redirect, placed.StatusCode);
        Assert.Equal("/checkout/review", placed.Headers.Location!.ToString());
        var review = await guest.GetStringAsync("/checkout/review");
        Assert.Equal(
            "Cannot place the order, 1 item(s) unavailable or out of stock.",
            AvailabilityPages.Text(review, @"data-test=""review-error-message"">([^<]*)</div>"));
        Assert.DoesNotContain("data-test=\"confirmation-title\"", review, StringComparison.Ordinal);
    }

    /// <summary>What another visitor's placed order does to the stock: the reduction its confirmation triggers in Inventory.</summary>
    private async Task AnotherVisitorHasBoughtAsync(string productPath, int quantity)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IReduceStockInputPort>()
            .ExecuteAsync(new ReduceStockCommand([new ReduceStockCommand.Line(Guid.Parse(productPath["/products/".Length..]), quantity)], Guid.NewGuid()));
    }
}