using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// Each page on the way to the checkout, on its own, at a phone's width (393 px): the document never scrolls
/// sideways.
/// </summary>
public sealed class ShopFitsAPhoneE2eTest : BaseE2eTest
{
    public ShopFitsAPhoneE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    protected override BrowserNewContextOptions ContextOptions => new() { ViewportSize = new ViewportSize { Width = 393, Height = 852 } };

    [E2eFact(DisplayName = "The catalogue fits a phone")]
    public async Task TheCataloguePageDoesNotScrollSidewaysOnAPhone()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);

        Assert.True(await catalog.FitsViewportAsync(), "Catalogue page scrolls sideways at 393 px");
    }

    [E2eFact(DisplayName = "A product page fits a phone")]
    public async Task AProductPageDoesNotScrollSidewaysOnAPhone()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);

        var product = await catalog.ViewProductAsync("Patterns of Enterprise Application Architecture");

        Assert.True(await product.FitsViewportAsync(), "Product page scrolls sideways at 393 px");
    }

    [E2eFact(DisplayName = "The cart fits a phone")]
    public async Task TheCartPageDoesNotScrollSidewaysOnAPhone()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");
        await product.AddToCartAsync();

        var cart = await CartPage.NavigateToAsync(Page);

        Assert.True(await cart.HasItemsAsync(), "Cart holds no line");
        Assert.True(await cart.FitsViewportAsync(), "Cart page scrolls sideways at 393 px");
    }

    [E2eFact(DisplayName = "The buyer step fits a phone")]
    public async Task TheBuyerStepDoesNotScrollSidewaysOnAPhone()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");
        await product.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);

        await cart.PressProceedToCheckoutAsync();

        Assert.Equal("/checkout/buyer", cart.ShownPath);
        var buyer = await BuyerInfoPage.OpenAsync(Page);
        Assert.True(await buyer.FitsViewportAsync(), "Buyer step scrolls sideways at 393 px");
    }
}