using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// Two guests press "Place Order" at the same moment for more of a product than the stock holds for both: one order is
/// placed, the other is refused at the review, and the stock goes down by the placed order only. The test runs a shop of
/// its own: it counts the seeded stock of the Hexagon exactly and uses it up.
/// </summary>
public sealed class TwoOrdersAtOnceE2eTest : BaseE2eTest
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    public TwoOrdersAtOnceE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [OwnShopFact(DisplayName = "The order the stock no longer covers is refused")]
    public async Task OfTwoOrdersPlacedAtOnceOnlyTheOneTheStockCoversIsPlaced()
    {
        using var shop = ShopOfItsOwn.Start();
        var hexagon = await (await ProductCatalogPage.NavigateToAsync(Page, shop.BaseUrl)).ProductPathAsync(Hexagon);
        Assert.Equal("8 units available", await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon)).AvailabilityAsync());
        await using var secondBrowser = await NewBrowserAsync();
        var secondGuest = await secondBrowser.NewPageAsync();
        var reviews = new List<(IPage Guest, ReviewPage Review)>();
        foreach (var guest in new[] { Page, secondGuest })
        {
            await PutInTheCartAsync(guest, shop.BaseUrl, hexagon, 5);
            reviews.Add((guest, await GuestFromTheCartToTheReviewAsync(guest, shop.BaseUrl)));
        }

        await Task.WhenAll(reviews.Select(placing => placing.Review.PlaceOrderAndAwaitTheAnswerAsync()));

        var confirmation = shop.BaseUrl + "/checkout/confirmation";
        var placed = Assert.Single(reviews, placing => placing.Guest.Url == confirmation);
        await Assertions.Expect(placed.Guest.Locator("[data-test='confirmation-title']")).ToBeVisibleAsync();
        var refused = Assert.Single(reviews, placing => placing.Guest.Url != confirmation);
        await Assertions.Expect(refused.Guest).ToHaveURLAsync(shop.BaseUrl + "/checkout/review");
        Assert.Equal(
            "Not enough stock left for 'Wooden Hexagon Desk Model'. Please adjust your order.",
            await refused.Review.ErrorMessageAsync());
        Assert.Equal("3 units available", await AvailabilityShortlyAfterwardsAsync(shop.BaseUrl, hexagon, "3 units available"));
    }

    /// <summary>Presses "Add to Cart" on the product page <paramref name="times"/> times in <paramref name="guest"/>'s browser.</summary>
    private static async Task PutInTheCartAsync(IPage guest, string shopUrl, string productPath, int times)
    {
        for (var unit = 0; unit < times; unit++)
        {
            await (await ProductDetailPage.NavigateToAsync(guest, shopUrl, productPath)).PressAddToCartAsync();
        }
    }

    /// <summary>
    /// Opens the product page again until it shows <paramref name="expected"/> or 15 seconds have passed, and answers
    /// what it showed last: the stock changes after the order, not at the same moment.
    /// </summary>
    private async Task<string> AvailabilityShortlyAfterwardsAsync(string shopUrl, string productPath, string expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var shown = await (await ProductDetailPage.NavigateToAsync(Page, shopUrl, productPath)).AvailabilityAsync();
            if (shown == expected || DateTime.UtcNow > deadline)
            {
                return shown;
            }

            await Page.WaitForTimeoutAsync(250);
        }
    }
}