using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// A guest places an order, and the cart keeps only what was added after the checkout started. The test runs a shop of
/// its own: the order uses up stock of seeded products that the suite's shared shop keeps for other tests.
/// </summary>
public sealed class CartAfterTheOrderE2eTest : BaseE2eTest
{
    private const string Pin = "\"Bounded Context\" Enamel Pin";
    private const string Hoodie = "\"Domain over Framework\" Hoodie";
    private const string TShirt = "\"Ports & Adapters\" T-Shirt";

    public CartAfterTheOrderE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [OwnShopFact(DisplayName = "Only the later addition remains")]
    public async Task PlacingTheOrderLeavesOnlyTheLineAddedAfterTheCheckoutStarted()
    {
        using var shop = ShopOfItsOwn.Start();
        await AddToCartAsync(shop.BaseUrl, Pin, 2);
        await AddToCartAsync(shop.BaseUrl, Hoodie, 1);
        await (await CartPage.NavigateToAsync(Page, shop.BaseUrl)).PressProceedToCheckoutAsync();
        Assert.Equal(2, (await (await BuyerInfoPage.OpenAsync(Page, shop.BaseUrl)).SummaryItemsAsync()).Count);
        await AddToCartAsync(shop.BaseUrl, TShirt, 1);
        Assert.Equal(3, (await (await CartPage.NavigateToAsync(Page, shop.BaseUrl)).LineItemsAsync()).Count);

        var review = await GuestFromTheBuyerStepToTheReviewAsync(shop.BaseUrl);
        await review.PlaceOrderAndAwaitTheAnswerAsync();

        await Assertions.Expect(Page).ToHaveURLAsync(shop.BaseUrl + "/checkout/confirmation");
        Assert.True(await review.ShowsConfirmationAsync(), "the order is confirmed");
        Assert.Equal(new[] { TShirt + " x1" }, await CartLinesOnceSettledAsync(shop.BaseUrl, 1));
    }

    private async Task AddToCartAsync(string shopUrl, string productName, int times)
    {
        var path = await (await ProductCatalogPage.NavigateToAsync(Page, shopUrl)).ProductPathAsync(productName);
        for (var i = 0; i < times; i++)
        {
            await (await ProductDetailPage.NavigateToAsync(Page, shopUrl, path)).PressAddToCartAsync();
        }
    }

    /// <summary>Opens the buyer step of the guest's checkout and goes through buyer, delivery and payment to the review.</summary>
    private async Task<ReviewPage> GuestFromTheBuyerStepToTheReviewAsync(string shopUrl)
    {
        await Page.GotoAsync(shopUrl + "/checkout/buyer");
        var buyer = await BuyerInfoPage.OpenAsync(Page, shopUrl);
        await buyer.FillBuyerInfoAsync("guest@example.com", "Test", "Guest", "+1-555-0100");
        await buyer.PressContinueToDeliveryAsync();
        var delivery = await DeliveryPage.OpenAsync(Page, shopUrl);
        await delivery.FillAddressAsync("123 Main Street", "Springfield", "12345", "United States", "IL");
        await delivery.ChooseShippingMethodAsync("Standard Shipping");
        await delivery.SubmitAndShowNextPathAsync();
        var payment = await PaymentPage.OpenAsync(Page, shopUrl);
        await payment.ChooseTheOfferedPaymentProviderAsync();
        await payment.SubmitAndShowNextPathAsync();
        return await ReviewPage.OpenAsync(Page, shopUrl);
    }

    /// <summary>The cart page's lines once they number <paramref name="expectedCount"/>, or after 15 seconds as they are then.</summary>
    private async Task<IReadOnlyList<string>> CartLinesOnceSettledAsync(string shopUrl, int expectedCount)
    {
        IReadOnlyList<string> lines = Array.Empty<string>();
        for (var attempt = 0; attempt < 30; attempt++)
        {
            lines = await (await CartPage.NavigateToAsync(Page, shopUrl)).LineItemsAsync();
            if (lines.Count == expectedCount)
            {
                return lines;
            }
            await Task.Delay(500);
        }
        return lines;
    }
}