using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>A guest's open checkout is never shown to a second visitor in another browser.</summary>
public sealed class CheckoutBetweenVisitorsE2eTest : BaseE2eTest
{
    public CheckoutBetweenVisitorsE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Another visitor finds no checkout")]
    public async Task AStrangerOpeningTheBuyerStepFindsTheirCartAndNotTheOwnersCheckout()
    {
        await OwnerOnTheDeliveryStepAsync();
        await using var strangersBrowser = await NewBrowserAsync();
        var stranger = await strangersBrowser.NewPageAsync();

        await stranger.GotoAsync(BaseUrl + "/checkout/buyer");

        var cart = await CartPage.OpenAsync(stranger);
        Assert.Equal("/cart?notice=no-active-checkout", cart.ShownPath);
        Assert.Equal("No active checkout session found", await cart.ErrorMessageAsync());
        Assert.Equal(0, await cart.ItemCountAsync());
        Assert.Equal(0, await stranger.GetByText("jane@example.com").CountAsync());
    }

    private async Task OwnerOnTheDeliveryStepAsync()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");
        await product.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        await (await buyer.FillBuyerInfoAsync("jane@example.com", "Jane", "Doe", "+49 30 1234567")).ContinueToDeliveryAsync();
    }
}