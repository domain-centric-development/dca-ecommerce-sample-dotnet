using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>What a visitor learns about a product's stock on its product page.</summary>
public sealed class ProductAvailabilityE2eTest : BaseE2eTest
{
    public ProductAvailabilityE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Product page shows units available")]
    public async Task ProductPageShowsUnitsAvailable()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);

        var product = await catalog.ViewProductAsync("Domain-Driven Design");

        Assert.Equal("In Stock", await product.StockBadgeAsync());
        Assert.Equal("20 units available", await product.AvailabilityAsync());
    }
}