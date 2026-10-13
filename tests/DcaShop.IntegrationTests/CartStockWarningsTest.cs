using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The warnings the cart page shows on a line once another guest's order has left less stock than the line holds,
/// through the real pages. Each test runs a host of its own: the orders use up the seeded stock of the Hexagon.
/// </summary>
public sealed class CartStockWarningsTest
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    [Fact]
    public async Task ALineWhoseProductSoldOutSaysOnlyThatItIsNoLongerAvailable()
    {
        using var factory = new WebApplicationFactory<Program>();
        var guest = GuestOrders.Guest(factory);
        var hexagon = await AvailabilityPages.ProductPathAsync(guest, Hexagon);
        Assert.Equal("8 units available", await GuestOrders.AvailabilityAsync(guest, hexagon));
        await AddAsync(guest, hexagon, 1);
        await GuestOrders.PlaceOrderAsync(GuestOrders.Guest(factory), (hexagon, 8));

        var cartPage = await CartPageOnceItWarnsAsync(guest);

        var line = Assert.Single(Lines(cartPage), l => LineName(l) == Hexagon);
        Assert.Contains("Product no longer available", StockWarnings(line));
        Assert.DoesNotContain("Insufficient stock available", StockWarnings(line));
    }

    [Fact]
    public async Task OnlyTheLineThatIsShortOfStockWarns()
    {
        using var factory = new WebApplicationFactory<Program>();
        var guest = GuestOrders.Guest(factory);
        var teamTopologies = await AvailabilityPages.ProductPathAsync(guest, "Team Topologies");
        var hexagon = await AvailabilityPages.ProductPathAsync(guest, Hexagon);
        await AddAsync(guest, teamTopologies, 1);
        await AddAsync(guest, hexagon, 2);
        await GuestOrders.PlaceOrderAsync(GuestOrders.Guest(factory), (hexagon, 7));

        var cartPage = await CartPageOnceItWarnsAsync(guest);

        var lines = Lines(cartPage);
        Assert.Equal(["Team Topologies", Hexagon], lines.Select(LineName).Order());
        var warned = Assert.Single(lines, l => StockWarnings(l).Count > 0);
        Assert.Equal(Hexagon, LineName(warned));
        Assert.Single(StockWarnings(warned));
    }

    private static async Task AddAsync(HttpClient guest, string productPath, int quantity)
    {
        var detail = await guest.GetStringAsync(productPath);
        var token = Regex.Match(detail, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        Assert.NotEmpty(token);
        var added = await guest.PostAsync("/cart/add-product", new FormUrlEncodedContent([
            new("productId", productPath["/products/".Length..]),
            new("quantity", quantity.ToString()),
            new("__RequestVerificationToken", token)]));
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);
    }

    /// <summary>
    /// Opens the cart page until it shows a stock warning or 15 seconds have passed, and answers what it showed last:
    /// the stock goes down after the order, not at the same moment.
    /// </summary>
    private static async Task<string> CartPageOnceItWarnsAsync(HttpClient guest)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var cartPage = await guest.GetStringAsync("/cart");
            if (cartPage.Contains("data-test=\"stock-alert\"", StringComparison.Ordinal) || DateTime.UtcNow > deadline)
            {
                return cartPage;
            }

            await Task.Delay(100);
        }
    }

    /// <summary>The markup of each cart line, in page order.</summary>
    private static IReadOnlyList<string> Lines(string cartPage) =>
        cartPage.Split("data-test=\"cart-item\"").Skip(1).ToList();

    private static string LineName(string line) => AvailabilityPages.Text(line, @"data-test=""cart-item-name"">([^<]*)<");

    /// <summary>The stock warnings a line shows, as written.</summary>
    private static IReadOnlyList<string> StockWarnings(string line) =>
        Regex.Matches(line, @"data-test=""stock-alert"">([^<]*)<").Select(m => WebUtility.HtmlDecode(m.Groups[1].Value).Trim()).ToList();
}