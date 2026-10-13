using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest fills a cart, logs in to an account whose cart holds nothing, and finds the cart there.</summary>
public sealed class GuestCartLoginE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";
    private const string Mug = "\"Ubiquitous Language\" Mug";

    public GuestCartLoginE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Guest cart follows into the account")]
    public async Task TheProductsAGuestCollectedAreInTheAccountsCartAfterLoggingIn()
    {
        // The browser suite shares one shop, so Anna's address carries a suffix of its own.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, Password, Password);
        await ClearCookiesAsync();
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync(Mug);
        await product.AddToCartAsync();

        var login = await LoginPage.NavigateToAsync(Page);
        await login.LoginAsync(email, Password);

        var cart = await CartPage.OpenAsync(Page);
        Assert.Equal("/cart", cart.ShownPath);
        Assert.Equal("Welcome back!", await cart.SuccessMessageAsync());
        Assert.Equal([$"{Mug} x1"], await cart.LineItemsAsync());
    }
}