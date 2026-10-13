using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>Hovering the header's cart entry shows what the cart holds and what it comes to.</summary>
public sealed class HeaderBasketE2eTest : BaseE2eTest
{
    public HeaderBasketE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Basket dropdown lists the cart")]
    public async Task HoveringTheHeaderBasketListsTheLinesTheirTotalAndAWayToTheCart()
    {
        await AddOneUnitAsync("Domain-Driven Design");
        await AddOneUnitAsync("Domain-Driven Design");
        await AddOneUnitAsync("Clean Architecture");
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);

        var basket = await catalog.HoverHeaderBasketAsync();

        Assert.Equal(
            new BasePage.HeaderBasketLine[] { new("Domain-Driven Design", "x2"), new("Clean Architecture", "x1") },
            basket.Lines);
        Assert.Equal(("Total", "149.97 EUR"), (basket.TotalLabel, basket.Total));
        Assert.Equal(("View Cart", "/cart"), (basket.ViewCartText, basket.ViewCartTarget));
    }

    private async Task AddOneUnitAsync(string productName)
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync(productName);
        await product.AddToCartAsync();
    }
}