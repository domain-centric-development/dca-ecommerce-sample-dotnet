using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest puts a product into the cart from its product page.</summary>
public sealed class AddToCartE2eTest : BaseE2eTest
{
    public AddToCartE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Add to cart lands on the cart")]
    public async Task AddToCartLandsOnTheCart()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");

        var cart = await product.AddToCartAsync();

        Assert.Equal("/cart?notice=product-added", cart.ShownPath);
        Assert.Equal("Product added to cart!", await cart.SuccessMessageAsync());
        Assert.Equal(["Domain-Driven Design x1"], await cart.LineItemsAsync());
    }
}