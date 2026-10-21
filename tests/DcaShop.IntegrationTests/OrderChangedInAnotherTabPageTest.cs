using System.Net;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A "Place Order" from a review shown before another tab of the same browser changed the checkout is refused: the
/// review again, at its own address, with the error and the order as it is now. The second tab is the same cookies.
/// </summary>
public sealed class OrderChangedInAnotherTabPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderChangedInAnotherTabPageTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task AShippingChosenInAnotherTabRefusesTheReviewedOrder()
    {
        var guest = GuestOrders.Guest(_factory);
        var ddd = await AvailabilityPages.ProductPathAsync(guest, "Domain-Driven Design");
        await GuestOrders.GoToTheReviewAsync(guest, (ddd, 1));
        var firstTab = await guest.GetStringAsync("/checkout/review");
        Assert.Equal("54.99 EUR", AvailabilityPages.Text(firstTab, @"class=""order-summary__item-price"">([^<]*)</span>"));
        Assert.Equal("Standard Shipping", ShippingMethod(firstTab));
        var secondTab = await guest.GetStringAsync("/checkout/delivery");
        var chosen = await guest.PostAsync("/checkout/delivery", GuestOrders.Form(secondTab,
            ("street", "Hauptstrasse 1"), ("city", "Berlin"), ("postalCode", "10115"), ("country", "Germany"), ("shippingOptionId", "express")));
        Assert.Equal(HttpStatusCode.Redirect, chosen.StatusCode);

        var placed = await guest.PostAsync("/checkout/confirm", GuestOrders.Form(firstTab, ("placedFrom", "review")));

        Assert.Equal(HttpStatusCode.Redirect, placed.StatusCode);
        Assert.Equal("/checkout/review", placed.Headers.Location!.ToString());
        var review = await guest.GetStringAsync(placed.Headers.Location);
        Assert.Equal(
            "Your order has changed since you reviewed it. Please check it and place it again.",
            AvailabilityPages.Text(review, @"data-test=""review-error-message"">([^<]*)</div>"));
        Assert.Equal("Express Shipping", ShippingMethod(review));
        Assert.DoesNotContain("data-test=\"confirmation-title\"", review, StringComparison.Ordinal);
    }

    /// <summary>The shipping method the review names under "Shipping Method".</summary>
    private static string ShippingMethod(string review) =>
        AvailabilityPages.Text(review, @"<h4>Shipping Method</h4>\s*<p>([^<]*)</p>");
}