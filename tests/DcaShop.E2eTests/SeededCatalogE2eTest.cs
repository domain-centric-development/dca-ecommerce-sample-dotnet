using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// The sample catalog the shop seeds at start-up: the same 21 products in both samples, listed by product name in
/// ordinal order. Same scenario as the Java sample's <c>SeededCatalogE2ETest</c>. The test runs a shop of its own:
/// tests in the suite's shared shop add products of their own to its catalogue.
/// </summary>
public sealed class SeededCatalogE2eTest : BaseE2eTest
{
    public SeededCatalogE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [OwnShopFact(DisplayName = "Catalogue lists the seeded range")]
    public async Task CatalogListsTheSeededProductsInNameOrder()
    {
        using var shop = ShopOfItsOwn.Start();
        var titles = await (await ProductCatalogPage.NavigateToAsync(Page, shop.BaseUrl)).ProductTitlesAsync();

        Assert.Equal(21, titles.Count);
        Assert.Equal(titles.Order(StringComparer.Ordinal), titles);
        Assert.Equal("\"Bounded Context\" Enamel Pin", titles[0]);
    }
}