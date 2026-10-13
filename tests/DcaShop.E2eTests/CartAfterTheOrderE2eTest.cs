using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest places an order, and the cart keeps only what was added after the checkout started.</summary>
public sealed class CartAfterTheOrderE2eTest : BaseE2eTest
{
    private const string Pin = "\"Bounded Context\" Enamel Pin";
    private const string Hoodie = "\"Domain over Framework\" Hoodie";
    private const string TShirt = "\"Ports & Adapters\" T-Shirt";

    public CartAfterTheOrderE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Only the later addition remains")]
    public async Task PlacingTheOrderLeavesOnlyTheLineAddedAfterTheCheckoutStarted()
    {
        await AddToCartAsync(Pin, 2);
        await AddToCartAsync(Hoodie, 1);
        var buyer = await (await CartPage.NavigateToAsync(Page)).ProceedToCheckoutAsync();
        Assert.Equal(2, (await buyer.SummaryItemsAsync()).Count);
        await AddToCartAsync(TShirt, 1);
        Assert.Equal(3, (await (await CartPage.NavigateToAsync(Page)).LineItemsAsync()).Count);

        await NavigateToAsync("/checkout/buyer");
        var delivery = await (await (await BuyerInfoPage.OpenAsync(Page)).FillBuyerInfoAsync("guest@example.com", "Test", "Guest", "+1-555-0100")).ContinueToDeliveryAsync();
        await delivery.FillAddressAsync("123 Main Street", "Springfield", "12345", "United States", "IL");
        await delivery.SelectFirstShippingOptionAsync();
        var payment = await delivery.ContinueToPaymentAsync();
        await payment.SelectFirstPaymentProviderAsync();
        var confirmation = await (await payment.ContinueToReviewAsync()).PlaceOrderAsync();

        Assert.True(await confirmation.IsOrderConfirmedAsync(), "the order is confirmed");
        Assert.Equal(new[] { TShirt + " x1" }, await CartLinesOnceSettledAsync(1));
    }

    private async Task AddToCartAsync(string productName, int times)
    {
        for (var i = 0; i < times; i++)
        {
            await (await (await ProductCatalogPage.NavigateToAsync(Page)).ViewProductAsync(productName)).AddToCartAsync();
        }
    }

    /// <summary>The cart page's lines once they number <paramref name="expectedCount"/>, or after 15 seconds as they are then.</summary>
    private async Task<IReadOnlyList<string>> CartLinesOnceSettledAsync(int expectedCount)
    {
        IReadOnlyList<string> lines = Array.Empty<string>();
        for (var attempt = 0; attempt < 30; attempt++)
        {
            lines = await (await CartPage.NavigateToAsync(Page)).LineItemsAsync();
            if (lines.Count == expectedCount)
            {
                return lines;
            }
            await Task.Delay(500);
        }
        return lines;
    }
}