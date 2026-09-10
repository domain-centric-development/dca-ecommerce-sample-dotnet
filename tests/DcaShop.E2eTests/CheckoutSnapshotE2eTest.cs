using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// Checkout snapshot policy through the browser: a checkout session is a copy of the submitted cart positions,
/// cart edits leave it untouched, a second checkout supersedes the open session, and a completed checkout
/// reconciles only the purchased snapshot — later additions survive. Same scenario, page objects and
/// <c>data-test</c> selectors as the Java sample's <c>CheckoutSnapshotE2ETest</c>.
/// </summary>
public sealed class CheckoutSnapshotE2eTest : BaseE2eTest
{
    private const int ProductA = 0;
    private const int ProductB = 1;
    private const int ProductC = 2;

    public CheckoutSnapshotE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Cart edits do not change the open checkout session; a new checkout supersedes it")]
    public async Task CartEditsLeaveTheSnapshotUntouchedAndRestartReplacesIt()
    {
        await AddToCartAsync(ProductA, 2);
        var productA = (await (await CartPage.NavigateToAsync(Page)).LineItemsAsync())[0];
        Assert.EndsWith(" x2", productA, StringComparison.Ordinal);
        var nameA = productA[..productA.LastIndexOf(" x", StringComparison.Ordinal)];

        var firstSession = await (await CartPage.NavigateToAsync(Page)).ProceedToCheckoutAsync();
        Assert.Equal(new[] { nameA + " x2" }, await firstSession.SummaryItemsAsync());

        // Edit the cart while the session is open: more of A, plus product B
        await AddToCartAsync(ProductA, 3);
        await AddToCartAsync(ProductB, 1);
        var cartLines = await (await CartPage.NavigateToAsync(Page)).LineItemsAsync();
        Assert.Equal(2, cartLines.Count);
        Assert.EndsWith(" x5", cartLines[0], StringComparison.Ordinal);

        await NavigateToAsync("/checkout/buyer");
        var stillFirstSession = await BuyerInfoPage.OpenAsync(Page);
        Assert.Equal(new[] { nameA + " x2" }, await stillFirstSession.SummaryItemsAsync());

        // A second checkout creates a fresh snapshot and supersedes the first session
        var secondSession = await (await CartPage.NavigateToAsync(Page)).ProceedToCheckoutAsync();
        var summary = await secondSession.SummaryItemsAsync();
        Assert.Equal(2, summary.Count);
        Assert.Equal(nameA + " x5", summary[0]);
    }

    [E2eFact(DisplayName = "Completing a checkout removes only the purchased snapshot from the cart")]
    public async Task CompletionReconcilesOnlyThePurchasedSnapshot()
    {
        await AddToCartAsync(ProductA, 2);
        await AddToCartAsync(ProductB, 1);
        var buyer = await (await CartPage.NavigateToAsync(Page)).ProceedToCheckoutAsync();
        Assert.Equal(2, (await buyer.SummaryItemsAsync()).Count);

        // A later addition that is not part of the snapshot
        await AddToCartAsync(ProductC, 1);
        var before = await (await CartPage.NavigateToAsync(Page)).LineItemsAsync();
        Assert.Equal(3, before.Count);
        var lineC = before[2];

        await NavigateToAsync("/checkout/buyer");
        var delivery = await (await (await BuyerInfoPage.OpenAsync(Page)).FillBuyerInfoAsync("snapshot@example.com", "Snap", "Shot", "+1-555-0100")).ContinueToDeliveryAsync();
        await delivery.FillAddressAsync("123 Main Street", "Springfield", "12345", "United States", "IL");
        await delivery.SelectFirstShippingOptionAsync();
        var payment = await delivery.ContinueToPaymentAsync();
        await payment.SelectFirstPaymentProviderAsync();
        var confirmation = await (await payment.ContinueToReviewAsync()).PlaceOrderAsync();
        Assert.True(await confirmation.IsOrderConfirmedAsync(), "Order should be confirmed");

        // Reconciliation is asynchronous (integration event); the later addition must survive
        var remaining = await WaitForCartLinesAsync(1);
        Assert.Equal(new[] { lineC }, remaining);

        // The cart stays editable after the purchase
        await AddToCartAsync(ProductB, 1);
        Assert.Equal(2, await (await CartPage.NavigateToAsync(Page)).ItemCountAsync());
    }

    private async Task AddToCartAsync(int productIndex, int times)
    {
        for (var i = 0; i < times; i++)
        {
            await (await (await ProductCatalogPage.NavigateToAsync(Page)).ViewProductAsync(productIndex)).AddToCartAsync();
        }
    }

    private async Task<IReadOnlyList<string>> WaitForCartLinesAsync(int expectedCount)
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
