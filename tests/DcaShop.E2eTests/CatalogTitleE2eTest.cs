using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// The catalogue page names itself in the browser tab the same way in both samples; the breadcrumb keeps its
/// wording.
/// </summary>
public sealed class CatalogTitleE2eTest : BaseE2eTest
{
    public CatalogTitleE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "The catalogue page is titled Product Catalog")]
    public async Task CatalogPageIsTitledProductCatalog()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);

        Assert.Equal("Product Catalog", await catalog.DocumentTitleAsync());
        Assert.Equal("Home / Products", await catalog.BreadcrumbAsync());
    }
}