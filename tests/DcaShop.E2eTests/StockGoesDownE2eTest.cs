using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// A placed order lowers the stock its product page shows, shortly after the order. The test runs a shop of its own:
/// it counts the seeded stock of the Hexagon exactly and uses some of it up, which the suite's shared shop keeps for
/// other tests.
/// </summary>
public sealed class StockGoesDownE2eTest : BaseE2eTest
{
    private const string Product = "Wooden Hexagon Desk Model";

    public StockGoesDownE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [OwnShopFact(DisplayName = "Stock goes down by the quantity bought")]
    public async Task PlacingTheOrderLowersTheStockOnTheProductPageByTheQuantityBought()
    {
        using var shop = ShopOfItsOwn.Start();
        var productPath = await (await ProductCatalogPage.NavigateToAsync(Page, shop.BaseUrl)).ProductPathAsync(Product);
        var product = await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, productPath);
        Assert.Equal("8 units available", await product.AvailabilityAsync());
        await product.PressAddToCartAsync();
        await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, productPath)).PressAddToCartAsync();
        var review = await GuestFromTheCartToTheReviewAsync(shop.BaseUrl);

        await review.PlaceOrderAndAwaitTheAnswerAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(shop.BaseUrl + "/checkout/confirmation");
        Assert.Equal("6 units available", await AvailabilityShortlyAfterwardsAsync(shop.BaseUrl, productPath, "6 units available"));
    }

    /// <summary>Proceeds from the cart through buyer, delivery and payment of the shop at <paramref name="shopUrl"/>.</summary>
    private async Task<ReviewPage> GuestFromTheCartToTheReviewAsync(string shopUrl)
    {
        await (await CartPage.NavigateToAsync(Page, shopUrl)).PressProceedToCheckoutAsync();
        var buyer = await BuyerInfoPage.OpenAsync(Page, shopUrl);
        await buyer.FillBuyerInfoAsync("jane@example.com", "Jane", "Doe", "+49 30 1234567");
        await buyer.PressContinueToDeliveryAsync();
        var delivery = await DeliveryPage.OpenAsync(Page, shopUrl);
        await delivery.FillAddressAsync("Hauptstrasse 1", "Berlin", "10115", "Germany", string.Empty);
        await delivery.ChooseShippingMethodAsync("Standard Shipping");
        await delivery.SubmitAndShowNextPathAsync();
        var payment = await PaymentPage.OpenAsync(Page, shopUrl);
        await payment.ChooseTheOfferedPaymentProviderAsync();
        await payment.SubmitAndShowNextPathAsync();
        return await ReviewPage.OpenAsync(Page, shopUrl);
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