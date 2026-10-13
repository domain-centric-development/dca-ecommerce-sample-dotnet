using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A placed order lowers the stock its product page shows, shortly after the order.</summary>
public sealed class StockGoesDownE2eTest : BaseE2eTest
{
    private const string Product = "Wooden Hexagon Desk Model";

    public StockGoesDownE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Stock goes down by the quantity bought")]
    public async Task PlacingTheOrderLowersTheStockOnTheProductPageByTheQuantityBought()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync(Product);
        var productPath = product.ShownPath;
        Assert.Equal("8 units available", await product.AvailabilityAsync());
        await product.AddToCartAsync();
        await (await ProductDetailPage.NavigateToAsync(Page, productPath)).AddToCartAsync();
        var review = await GuestFromTheCartToTheReviewAsync();

        await review.PlaceOrderAsync();

        Assert.Equal("6 units available", await AvailabilityShortlyAfterwardsAsync(productPath, "6 units available"));
    }

    private async Task<ReviewPage> GuestFromTheCartToTheReviewAsync()
    {
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        var delivery = await (await buyer.FillBuyerInfoAsync("jane@example.com", "Jane", "Doe", "+49 30 1234567")).ContinueToDeliveryAsync();
        await delivery.FillAddressAsync("Hauptstrasse 1", "Berlin", "10115", "Germany", string.Empty);
        await delivery.ChooseShippingMethodAsync("Standard Shipping");
        var payment = await delivery.ContinueToPaymentAsync();
        await payment.ChooseTheOfferedPaymentProviderAsync();
        return await payment.ContinueToReviewAsync();
    }

    /// <summary>
    /// Opens the product page again until it shows <paramref name="expected"/> or 15 seconds have passed, and answers
    /// what it showed last: the stock changes after the order, not at the same moment.
    /// </summary>
    private async Task<string> AvailabilityShortlyAfterwardsAsync(string productPath, string expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var shown = await (await ProductDetailPage.NavigateToAsync(Page, productPath)).AvailabilityAsync();
            if (shown == expected || DateTime.UtcNow > deadline)
            {
                return shown;
            }

            await Page.WaitForTimeoutAsync(250);
        }
    }
}