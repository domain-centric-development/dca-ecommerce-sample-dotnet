using DcaShop.E2eTests.Pages;
using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// The shop on a phone: at a 393 px viewport (iPhone 14 Pro) the catalog, the cart and the checkout entry fit
/// the screen — the document never scrolls sideways. Same scenario and page objects as the Java sample's
/// <c>MobileLayoutE2ETest</c>.
/// </summary>
public sealed class MobileLayoutE2eTest : BaseE2eTest
{
    public MobileLayoutE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    protected override BrowserNewContextOptions ContextOptions => new() { ViewportSize = new ViewportSize { Width = 393, Height = 852 } };

    [E2eFact(DisplayName = "On a phone the catalog, a product and the cart fit the screen without sideways scrolling")]
    public async Task ShopFitsAPhoneViewport()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        Assert.True(await catalog.FitsViewportAsync(), "Catalog page scrolls sideways at 393 px");

        var product = await catalog.ViewFirstProductAsync();
        Assert.True(await product.FitsViewportAsync(), "Product page scrolls sideways at 393 px");

        var cart = await product.AddToCartAsync();
        Assert.True(await cart.FitsViewportAsync(), "Cart page scrolls sideways at 393 px");

        var buyer = await cart.ProceedToCheckoutAsync();
        Assert.True(await buyer.FitsViewportAsync(), "Checkout page scrolls sideways at 393 px");
    }
}
