using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// "Place Order" checks the stock again: a line that ran short since the checkout started keeps the guest at the review
/// with an error, and nothing changes. The test runs a shop of its own: the order it arranges uses up stock of the
/// Hexagon that the suite's shared shop keeps for other tests.
/// </summary>
public sealed class PlaceOrderRefusedE2eTest : BaseE2eTest
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    public PlaceOrderRefusedE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [RestartingFact(DisplayName = "Place order is refused when stock ran short")]
    public async Task PlaceOrderForMoreThanTheStockLeftKeepsTheReviewOpenAndChangesNothing()
    {
        using var shop = ShopOfItsOwn.Start();
        var hexagon = await (await ProductCatalogPage.NavigateToAsync(Page, shop.BaseUrl)).ProductPathAsync(Hexagon);
        for (var unit = 0; unit < 5; unit++)
        {
            await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon)).PressAddToCartAsync();
        }

        var review = await GuestFromTheCartToTheReviewAsync(shop.BaseUrl);
        await shop.AnotherGuestHasBoughtAsync(hexagon, 5);

        await review.PressPlaceOrderAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(System.Text.RegularExpressions.Regex.Escape(shop.BaseUrl + "/checkout/review") + @"(\?.*)?$"));
        Assert.NotEmpty(await review.ErrorMessageAsync());
        Assert.False(await review.ShowsConfirmationAsync());
        Assert.Equal("3 units available", await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon)).AvailabilityAsync());
        Assert.Equal("5", (await (await CartPage.NavigateToAsync(Page, shop.BaseUrl)).LineAsync(Hexagon)).Quantity);
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
}