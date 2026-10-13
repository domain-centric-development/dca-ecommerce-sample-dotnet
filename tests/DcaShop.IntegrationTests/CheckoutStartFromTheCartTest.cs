using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// What the cart page offers and answers when its cart can no longer be checked out as it was — short of stock since
/// another guest's order, or emptied by the guest's own order — through the real pages. Each test runs a host of its
/// own: the orders use up seeded stock.
/// </summary>
public sealed class CheckoutStartFromTheCartTest
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    [Fact]
    public async Task ACartShortOfStockOffersNoCheckoutAndSaysWhy()
    {
        using var factory = new WebApplicationFactory<Program>();
        var guest = GuestOrders.Guest(factory);
        var hexagon = await AvailabilityPages.ProductPathAsync(guest, Hexagon);
        Assert.Equal("8 units available", await GuestOrders.AvailabilityAsync(guest, hexagon));
        await AddAsync(guest, hexagon, 5);
        await GuestOrders.PlaceOrderAsync(GuestOrders.Guest(factory), (hexagon, 5));
        Assert.Equal("3 units available", await GuestOrders.AvailabilityShortlyAfterwardsAsync(guest, hexagon, "3 units available"));

        var cartPage = await guest.GetStringAsync("/cart");

        Assert.DoesNotContain("data-test=\"cart-checkout-button\"", cartPage, StringComparison.Ordinal);
        Assert.Equal("Cannot Checkout", AvailabilityPages.Text(cartPage, @"<button [^>]*\bdisabled\b[^>]*data-test=""cart-checkout-disabled"">([^<]*)</button>"));
        Assert.Equal("Some items are unavailable or have insufficient stock", AvailabilityPages.Text(cartPage, @"class=""cart__actions-warning"">([^<]*)</div>"));
    }

    [Fact]
    public async Task StartingFromACartEmptiedByTheOrderIsRefused()
    {
        using var factory = new WebApplicationFactory<Program>();
        var guest = GuestOrders.Guest(factory);
        await AddAsync(guest, await AvailabilityPages.ProductPathAsync(guest, "Domain-Driven Design"), 1);
        var oldCartPage = await guest.GetStringAsync("/cart");
        var cartId = Regex.Match(oldCartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        Assert.Equal("Proceed to Checkout", AvailabilityPages.Text(oldCartPage, @"data-test=""cart-checkout-button"">([^<]*)</button>"));
        await GuestOrders.PlaceOrderAsync(guest);
        await CartPageOnceEmptyAsync(guest);

        var started = await guest.PostAsync("/checkout/start", GuestOrders.Form(oldCartPage, ("cartId", cartId)));

        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);
        Assert.Equal("/cart", started.Headers.Location!.ToString());
        Assert.Equal(
            "Cannot check out an empty cart.",
            AvailabilityPages.Text(await guest.GetStringAsync("/cart"), @"data-test=""cart-error-message"">([^<]*)</div>"));
    }

    private static async Task AddAsync(HttpClient guest, string productPath, int quantity)
    {
        var added = await guest.PostAsync("/cart/add-product",
            GuestOrders.Form(await guest.GetStringAsync(productPath), ("productId", productPath["/products/".Length..]), ("quantity", quantity.ToString())));
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
    }

    /// <summary>Opens the cart page until it holds no line or 15 seconds have passed: the order empties it shortly after.</summary>
    private static async Task CartPageOnceEmptyAsync(HttpClient guest)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while ((await guest.GetStringAsync("/cart")).Contains("data-test=\"cart-item\"", StringComparison.Ordinal))
        {
            Assert.True(DateTime.UtcNow < deadline, "the cart is empty after the order");
            await Task.Delay(100);
        }
    }
}