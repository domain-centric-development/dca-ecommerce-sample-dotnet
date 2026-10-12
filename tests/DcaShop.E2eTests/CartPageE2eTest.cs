using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>The cart page shows what each line costs.</summary>
public sealed class CartPageE2eTest : BaseE2eTest
{
    public CartPageE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Cart shows lines with prices")]
    public async Task EachLineShowsItsImageQuantityUnitPriceAndLineTotal()
    {
        await AddOneUnitAsync("Domain-Driven Design");
        await AddOneUnitAsync("Domain-Driven Design");
        await AddOneUnitAsync("Clean Architecture");

        var cart = await CartPage.NavigateToAsync(Page);

        Assert.Equal(
            new CartPage.CartLine("/images/products/ddd-book.webp", "2", "54.99 EUR", "109.98 EUR"),
            await cart.LineAsync("Domain-Driven Design"));
        Assert.Equal(
            new CartPage.CartLine("/images/products/clean-architecture-book.webp", "1", "39.99 EUR", "39.99 EUR"),
            await cart.LineAsync("Clean Architecture"));
    }

    private async Task AddOneUnitAsync(string productName)
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync(productName);
        await product.AddToCartAsync();
    }
}