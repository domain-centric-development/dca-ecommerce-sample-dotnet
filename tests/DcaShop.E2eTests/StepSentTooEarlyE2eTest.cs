using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A step's form sent before the earlier steps are done leads the guest to the step that is still open.</summary>
public sealed class StepSentTooEarlyE2eTest : BaseE2eTest
{
    public StepSentTooEarlyE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Delivery sent before the buyer step")]
    public async Task ADeliveryFormSentBeforeTheBuyerStepEndsOnTheBuyerStepAndKeepsTheReviewClosed()
    {
        var buyer = await GuestWhoJustStartedTheCheckoutAsync();

        var shownPath = await buyer.SendDeliveryFormAsync("Hauptstrasse 1", "Berlin", "10115", "Germany", "standard");

        Assert.Equal("/checkout/buyer", shownPath);
        await NavigateToAsync("/checkout/review");
        var reopened = await BuyerInfoPage.OpenAsync(Page);
        Assert.Equal("/checkout/buyer", reopened.ShownPath);
    }

    private async Task<BuyerInfoPage> GuestWhoJustStartedTheCheckoutAsync()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");
        await product.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        return await cart.ProceedToCheckoutAsync();
    }
}