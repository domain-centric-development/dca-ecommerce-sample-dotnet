using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// A guest whose cart already holds every unit the shop has presses "Add to Cart" once more. The test runs a shop of
/// its own: it counts the seeded stock of the Hexagon exactly, which the suite's shared shop does not keep for it.
/// </summary>
public sealed class AddPastTheStockE2eTest : BaseE2eTest
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    public AddPastTheStockE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [OwnShopFact(DisplayName = "Adding past the stock on the page is refused")]
    public async Task AddingOneUnitMoreThanTheStockIsRefusedAndTheLineKeepsItsQuantity()
    {
        using var shop = ShopOfItsOwn.Start();
        var hexagon = await (await ProductCatalogPage.NavigateToAsync(Page, shop.BaseUrl)).ProductPathAsync(Hexagon);
        Assert.Equal("8 units available", await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon)).AvailabilityAsync());
        for (var unit = 0; unit < 8; unit++)
        {
            await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon)).PressAddToCartAsync();
        }

        Assert.Equal("8", (await (await CartPage.NavigateToAsync(Page, shop.BaseUrl)).LineAsync(Hexagon)).Quantity);

        await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, hexagon)).PressAddToCartAsync();

        var cart = await CartPage.OpenAsync(Page);
        Assert.Equal("/cart", new Uri(Page.Url).AbsolutePath);
        Assert.Equal(
            "There is not enough stock to add 1 of this product — your cart already holds 8, and 8 are in stock.",
            await cart.ErrorMessageAsync());
        Assert.Equal("8", (await cart.LineAsync(Hexagon)).Quantity);
    }
}