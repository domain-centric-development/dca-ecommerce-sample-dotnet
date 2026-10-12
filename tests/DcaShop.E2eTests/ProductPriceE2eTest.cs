using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>The price a visitor sees on a product's catalogue card, as the price owner set it at creation.</summary>
public sealed class ProductPriceE2eTest : BaseE2eTest
{
    public ProductPriceE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Card shows the price")]
    public async Task TheCardOfASeededProductShowsItsPrice()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);

        Assert.Equal("54.99 EUR", await catalog.CardPriceAsync("Domain-Driven Design"));
    }
}