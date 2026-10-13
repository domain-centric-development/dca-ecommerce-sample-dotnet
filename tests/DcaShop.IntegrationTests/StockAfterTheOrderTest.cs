using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// What the product page says about the stock once guests have placed orders through the real pages: each bought line
/// lowers its own product's stock, and orders add up. The products bought here are bought by no other test of this
/// class, so each starts at its seeded stock.
/// </summary>
public sealed class StockAfterTheOrderTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StockAfterTheOrderTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EachBoughtLineLowersTheStockOfItsOwnProduct()
    {
        var guest = GuestOrders.Guest(_factory);
        var ddd = await AvailabilityPages.ProductPathAsync(guest, "Domain-Driven Design");
        var cleanArchitecture = await AvailabilityPages.ProductPathAsync(guest, "Clean Architecture");
        Assert.Equal("20 units available", await GuestOrders.AvailabilityAsync(guest, ddd));
        Assert.Equal("35 units available", await GuestOrders.AvailabilityAsync(guest, cleanArchitecture));

        await GuestOrders.PlaceOrderAsync(guest, (ddd, 1), (cleanArchitecture, 2));

        Assert.Equal("19 units available", await GuestOrders.AvailabilityShortlyAfterwardsAsync(guest, ddd, "19 units available"));
        Assert.Equal("33 units available", await GuestOrders.AvailabilityShortlyAfterwardsAsync(guest, cleanArchitecture, "33 units available"));
    }

    [Fact]
    public async Task ASecondOrderLowersTheStockTheFirstOrderLeft()
    {
        var firstGuest = GuestOrders.Guest(_factory);
        var hexagon = await AvailabilityPages.ProductPathAsync(firstGuest, "Wooden Hexagon Desk Model");
        await GuestOrders.PlaceOrderAsync(firstGuest, (hexagon, 2));
        Assert.Equal("6 units available", await GuestOrders.AvailabilityShortlyAfterwardsAsync(firstGuest, hexagon, "6 units available"));
        var secondGuest = GuestOrders.Guest(_factory);

        await GuestOrders.PlaceOrderAsync(secondGuest, (hexagon, 3));

        Assert.Equal("3 units available", await GuestOrders.AvailabilityShortlyAfterwardsAsync(secondGuest, hexagon, "3 units available"));
    }
}

/// <summary>A guest who fills a cart and places the order through the checkout pages, and what the product page shows after.</summary>
internal static class GuestOrders
{
    /// <summary>A new visitor: a client with cookies of its own, redirects left to the caller.</summary>
    public static HttpClient Guest(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    public static async Task<string> AvailabilityAsync(HttpClient guest, string productPath) =>
        AvailabilityPages.Availability(await guest.GetStringAsync(productPath));

    /// <summary>
    /// Reads the product page until it shows <paramref name="expected"/> or 15 seconds have passed, and answers what it
    /// showed last: the stock changes after the order, not at the same moment.
    /// </summary>
    public static async Task<string> AvailabilityShortlyAfterwardsAsync(HttpClient guest, string productPath, string expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var shown = await AvailabilityAsync(guest, productPath);
            if (shown == expected || DateTime.UtcNow > deadline)
            {
                return shown;
            }

            await Task.Delay(100);
        }
    }

    /// <summary>Puts each product in the cart with its quantity, goes through the checkout and presses "Place Order".</summary>
    public static async Task PlaceOrderAsync(HttpClient guest, params (string ProductPath, int Quantity)[] lines)
    {
        foreach (var (productPath, quantity) in lines)
        {
            var detail = await guest.GetStringAsync(productPath);
            var added = await guest.PostAsync("/cart/add-product",
                Form(detail, ("productId", productPath["/products/".Length..]), ("quantity", quantity.ToString())));
            Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
        }

        var cartPage = await guest.GetStringAsync("/cart");
        var cartId = Regex.Match(cartPage, @"name=""cartId"" value=""([0-9a-f-]{36})""").Groups[1].Value;
        var started = await guest.PostAsync("/checkout/start", Form(cartPage, ("cartId", cartId)));
        Assert.Equal(HttpStatusCode.Redirect, started.StatusCode);

        await StepAsync(guest, "/checkout/buyer", "/checkout/delivery",
            ("email", "jane@example.com"), ("firstName", "Jane"), ("lastName", "Doe"), ("phone", "+49 30 1234567"));
        await StepAsync(guest, "/checkout/delivery", "/checkout/payment",
            ("street", "Hauptstrasse 1"), ("city", "Berlin"), ("postalCode", "10115"), ("country", "Germany"), ("shippingOptionId", "standard"));
        await StepAsync(guest, "/checkout/payment", "/checkout/review", ("providerId", "mock"));
        var confirmed = await guest.PostAsync("/checkout/confirm", Form(await guest.GetStringAsync("/checkout/review")));
        Assert.Equal(HttpStatusCode.Redirect, confirmed.StatusCode);
        Assert.Equal("/checkout/confirmation", confirmed.Headers.Location!.ToString());
    }

    private static async Task StepAsync(HttpClient guest, string url, string expectedNext, params (string, string)[] fields)
    {
        var response = await guest.PostAsync(url, Form(await guest.GetStringAsync(url), fields));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedNext, response.Headers.Location!.ToString());
    }

    private static FormUrlEncodedContent Form(string renderedPage, params (string Key, string Value)[] fields)
    {
        var token = Regex.Match(renderedPage, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        return new(fields.Append(("__RequestVerificationToken", token)).Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));
    }
}