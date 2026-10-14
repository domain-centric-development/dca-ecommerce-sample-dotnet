using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// The cart page tells a guest which line has less stock than it holds. The test runs a shop of its own: the order it
/// arranges uses up stock of the Hexagon that the suite's shared shop keeps for other tests.
/// </summary>
public sealed class CartStockWarningE2eTest : BaseE2eTest
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    public CartStockWarningE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [OwnShopFact(DisplayName = "Short stock line warns")]
    public async Task ALineHoldingMoreThanTheStockLeftWarnsOfInsufficientStock()
    {
        using var shop = ShopOfItsOwn.Start();
        var catalog = await ProductCatalogPage.NavigateToAsync(Page, shop.BaseUrl);
        var hexagon = await catalog.ProductPathAsync(Hexagon);
        var product = await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon);
        Assert.Equal("8 units available", await product.AvailabilityAsync());
        await product.PressAddToCartAsync();
        await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon)).PressAddToCartAsync();
        await shop.AnotherGuestHasBoughtAsync(hexagon, 7);
        Assert.Equal("1 units available", await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon)).AvailabilityAsync());

        var cart = await CartPage.NavigateToAsync(Page, shop.BaseUrl);

        Assert.Equal("2", (await cart.LineAsync(Hexagon)).Quantity);
        Assert.Equal(["Insufficient stock available"], await cart.StockWarningsAsync(Hexagon));
    }
}