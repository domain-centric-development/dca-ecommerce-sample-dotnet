using System.Text;

using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// "Place Order" orders only what the review showed: a price changed since refuses it, and the review shows today's
/// price. The change goes into a shop of the test's own, so the shared shop keeps the seeded prices the other suites read.
/// </summary>
public sealed class PlaceOrderAtTheReviewsPriceE2eTest : BaseE2eTest
{
    private const string DomainDrivenDesign = "Domain-Driven Design";

    public PlaceOrderAtTheReviewsPriceE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [OwnShopFact(DisplayName = "Place Order after a price change since the review is refused and the review shows today's price")]
    public async Task APriceChangedWhileReviewingRefusesTheOrderAndShowsTodaysPrice()
    {
        using var shop = SideShop.Start();
        var productPath = await (await ProductCatalogPage.NavigateToAsync(Page, shop.BaseUrl)).ProductPathAsync(DomainDrivenDesign);
        await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, productPath)).PressAddToCartAsync();
        var review = await GuestFromTheCartToTheReviewAsync(shop.BaseUrl);
        Assert.Equal(new[] { "Domain-Driven Design x1 54.99 EUR" }, await review.OrderLinesAsync());
        using var staff = Staff.Client(shop.BaseUrl, await Staff.TokenAsync(shop.BaseUrl, "price-keeper@example.com"));
        var changed = await staff.PutAsync($"/api/products/{productPath["/products/".Length..]}/price", new StringContent(
            """{"price":49.99}""", Encoding.UTF8, "application/json"));
        Assert.True(changed.IsSuccessStatusCode, $"the change is accepted, answered {(int)changed.StatusCode}");

        await review.PlaceOrderAndAwaitTheAnswerAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(shop.BaseUrl + "/checkout/review");
        var refused = await ReviewPage.OpenAsync(Page, shop.BaseUrl);
        Assert.Equal(
            "The price of 'Domain-Driven Design' has changed. Please check your order and place it again.",
            await refused.ErrorMessageAsync());
        Assert.Equal(new[] { "Domain-Driven Design x1 49.99 EUR" }, await refused.OrderLinesAsync());
        Assert.Equal("54.98 EUR", await refused.TotalAsync());
        Assert.False(await refused.ShowsConfirmationAsync());
    }

    /// <summary>Proceeds from the cart through buyer, delivery with "Standard Shipping" and payment of the shop at <paramref name="shopUrl"/>.</summary>
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