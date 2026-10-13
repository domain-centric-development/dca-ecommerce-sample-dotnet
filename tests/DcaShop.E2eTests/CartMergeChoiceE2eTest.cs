using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// A guest logs in to an account whose cart holds items too, and merges the two carts: the same product's quantities
/// add up, the other lines come along.
/// </summary>
public sealed class CartMergeChoiceE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";
    private const string CleanArchitecture = "Clean Architecture";
    private const string Mug = "\"Ubiquitous Language\" Mug";

    public CartMergeChoiceE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Merge both adds up the carts")]
    public async Task MergingBothCartsAddsUpTheQuantitiesOfTheSameProduct()
    {
        // The browser suite shares one shop, so Anna's address carries a suffix of its own.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, Password, Password);
        await AddToCartAsync(CleanArchitecture);
        await ClearCookiesAsync();
        await AddToCartAsync(CleanArchitecture);
        await AddToCartAsync(CleanArchitecture);
        await AddToCartAsync(Mug);
        var login = await LoginPage.NavigateToAsync(Page);
        await login.LoginAsync(email, Password);
        var merge = new CartMergePage(Page);
        Assert.True(merge.IsOnMergePage, $"Should be on the merge page. Got: {CurrentPath}");

        var cart = await merge.MergeBothCartsAsync();

        Assert.Equal("/cart", cart.ShownPath);
        Assert.Equal("Carts merged successfully!", await cart.SuccessMessageAsync());
        Assert.Equal([$"{CleanArchitecture} x3", $"{Mug} x1"], await cart.LineItemsAsync());
    }

    private async Task AddToCartAsync(string productName)
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync(productName);
        await product.AddToCartAsync();
    }
}