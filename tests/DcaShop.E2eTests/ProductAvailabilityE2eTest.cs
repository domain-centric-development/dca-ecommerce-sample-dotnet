using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// What a visitor learns about a product's stock on its product page. The test runs a shop of its own: it reads the
/// seeded stock exactly, and orders in the suite's shared shop use that stock up.
/// </summary>
public sealed class ProductAvailabilityE2eTest : BaseE2eTest
{
    public ProductAvailabilityE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [OwnShopFact(DisplayName = "Product page shows units available")]
    public async Task ProductPageShowsUnitsAvailable()
    {
        using var shop = ShopOfItsOwn.Start();
        var catalog = await ProductCatalogPage.NavigateToAsync(Page, shop.BaseUrl);

        var product = await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, await catalog.ProductPathAsync("Domain-Driven Design"));

        Assert.Equal("In Stock", await product.StockBadgeAsync());
        Assert.Equal("20 units available", await product.AvailabilityAsync());
    }
}