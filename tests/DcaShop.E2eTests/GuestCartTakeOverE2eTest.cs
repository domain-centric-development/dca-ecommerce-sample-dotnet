using DcaShop.E2eTests.Pages;

using Microsoft.Playwright;

namespace DcaShop.E2eTests;

/// <summary>
/// A guest in a browser that runs no script fills a cart, logs in to an account whose cart holds nothing, and takes the
/// cart over with the button of the page that follows the login.
/// </summary>
public sealed class GuestCartTakeOverE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";
    private const string Mug = "\"Ubiquitous Language\" Mug";

    public GuestCartTakeOverE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    // Without scripts the page transition between two pages of the shop holds up the navigation a form sends; a visitor
    // who asked for reduced motion gets no transition, which is what lets the browser move on.
    protected override BrowserNewContextOptions? ContextOptions => new() { JavaScriptEnabled = false, ReducedMotion = ReducedMotion.Reduce };

    [E2eFact(DisplayName = "Continue takes the guest cart over")]
    public async Task PressingContinueAfterTheLoginBringsTheGuestsCartIntoTheAccount()
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
        var takeOver = CartTakeOverPage.Of(Page);
        Assert.Equal("Bringing your cart along.", await takeOver.MessageAsync());

        var cart = await takeOver.ContinueAsync();

        Assert.Equal("/cart", cart.ShownPath);
        Assert.Equal("Welcome back!", await cart.SuccessMessageAsync());
        Assert.Equal([$"{Mug} x1"], await cart.LineItemsAsync());
    }
}