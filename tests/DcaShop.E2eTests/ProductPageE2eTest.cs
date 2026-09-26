using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>The product page as a visitor reaches it: from the product's card in the catalogue.</summary>
public sealed class ProductPageE2eTest : BaseE2eTest
{
    public ProductPageE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "View Details on a catalogue card opens that product's page")]
    public async Task ViewDetailsOnACatalogueCardOpensThatProductsPage()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);

        var product = await catalog.ViewProductAsync("Domain-Driven Design");

        Assert.Matches("^/products/[0-9a-f-]{36}$", product.ShownPath);
        Assert.Equal("Domain-Driven Design", await product.TitleAsync());
    }
}