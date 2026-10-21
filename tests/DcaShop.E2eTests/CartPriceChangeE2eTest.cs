using System.Text;

using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// A guest's cart after a member of staff changed the price of a product it holds. The change goes into a shop of the
/// test's own, so the shared shop keeps the seeded prices the other suites read.
/// </summary>
public sealed class CartPriceChangeE2eTest : BaseE2eTest
{
    private const string DomainDrivenDesign = "Domain-Driven Design";

    public CartPriceChangeE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [OwnShopFact(DisplayName = "The cart shows todays price after a change")]
    public async Task AfterAPriceChangeTheCartShowsTodaysPriceAndSaysItChanged()
    {
        using var shop = SideShop.Start();
        var catalog = await ProductCatalogPage.NavigateToAsync(Page, shop.BaseUrl);
        var path = await catalog.ProductPathAsync(DomainDrivenDesign);
        await (await ProductDetailPage.NavigateToAsync(Page, shop.BaseUrl, path)).PressAddToCartAsync();
        var added = await CartPage.NavigateToAsync(Page, shop.BaseUrl);
        Assert.Equal("54.99 EUR", (await added.LineAsync(DomainDrivenDesign)).UnitPrice);
        var staff = Staff.Client(shop.BaseUrl, await Staff.TokenAsync(shop.BaseUrl, "price-keeper@example.com"));

        var response = await staff.PutAsync($"/api/products/{path["/products/".Length..]}/price", new StringContent(
            """{"price":49.99}""", Encoding.UTF8, "application/json"));

        Assert.True(response.IsSuccessStatusCode, $"the change is accepted, answered {(int)response.StatusCode}");
        var cart = await CartPage.NavigateToAsync(Page, shop.BaseUrl);
        Assert.Equal("49.99 EUR", (await cart.LineAsync(DomainDrivenDesign)).UnitPrice);
        Assert.Equal(["Price decreased by 5.00 EUR"], await cart.PriceChangesAsync(DomainDrivenDesign));
        Assert.Equal("Some items have price changes since they were added to your cart.", await cart.PriceChangeNoticeAsync());
    }
}