using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest keeps changing the cart while a checkout is open, and the checkout keeps the order it was started with.</summary>
public sealed class OpenCheckoutKeepsItsOrderE2eTest : BaseE2eTest
{
    private const string Pin = "\"Bounded Context\" Enamel Pin";
    private const string Hoodie = "\"Domain over Framework\" Hoodie";

    public OpenCheckoutKeepsItsOrderE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Open checkout keeps its summary")]
    public async Task CartChangesLeaveTheOpenCheckoutSummaryAsItWasStarted()
    {
        await AddToCartAsync(Pin, 2);
        await (await CartPage.NavigateToAsync(Page)).ProceedToCheckoutAsync();
        await AddToCartAsync(Pin, 3);
        await AddToCartAsync(Hoodie, 1);
        var cartLines = await (await CartPage.NavigateToAsync(Page)).LineItemsAsync();
        Assert.Equal(2, cartLines.Count);
        Assert.Contains(Pin + " x5", cartLines);

        await NavigateToAsync("/checkout/buyer");

        var buyer = await BuyerInfoPage.OpenAsync(Page);
        Assert.Equal(new[] { Pin + " x2" }, await buyer.SummaryItemsAsync());
    }

    private async Task AddToCartAsync(string productName, int times)
    {
        for (var i = 0; i < times; i++)
        {
            await (await (await ProductCatalogPage.NavigateToAsync(Page)).ViewProductAsync(productName)).AddToCartAsync();
        }
    }
}