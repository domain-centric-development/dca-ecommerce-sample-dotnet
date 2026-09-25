using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// The sample catalog the shop seeds at start-up: the same 21 products in both samples, listed by product name in
/// ordinal order. Same scenario as the Java sample's <c>SeededCatalogE2ETest</c>.
/// </summary>
public sealed class SeededCatalogE2eTest : BaseE2eTest
{
    public SeededCatalogE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "The catalog lists the seeded products in name order")]
    public async Task CatalogListsTheSeededProductsInNameOrder()
    {
        var titles = await (await ProductCatalogPage.NavigateToAsync(Page)).ProductTitlesAsync();

        Assert.Equal(21, titles.Count);
        Assert.Equal(titles.Order(StringComparer.Ordinal), titles);
        Assert.Equal("\"Bounded Context\" Enamel Pin", titles[0]);
    }
}