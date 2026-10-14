using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest takes a line out of the cart on the cart page.</summary>
public sealed class RemoveFromCartE2eTest : BaseE2eTest
{
    public RemoveFromCartE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Remove takes the line out")]
    public async Task RemoveTakesTheLineOut()
    {
        await AddOneUnitAsync("Domain-Driven Design");
        await AddOneUnitAsync("Clean Architecture");
        await AddOneUnitAsync("Team Topologies");
        var cart = await CartPage.NavigateToAsync(Page);

        await cart.RemoveAsync("Clean Architecture");

        Assert.Equal(["Domain-Driven Design x1", "Team Topologies x1"], await cart.LineItemsAsync());
        Assert.Equal("2", await cart.HeaderCartBadgeAsync());
    }

    private async Task AddOneUnitAsync(string productName)
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync(productName);
        await product.AddToCartAsync();
    }
}