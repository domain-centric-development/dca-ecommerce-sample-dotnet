using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// A guest at the buyer step logs in to an account whose cart holds nothing; the cart is taken over and the checkout
/// opens again with the guest's order.
/// </summary>
public sealed class LoginFromTheCheckoutE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";
    private const string DomainDrivenDesign = "Domain-Driven Design";

    public LoginFromTheCheckoutE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "A guest who logs in from the buyer step returns to the checkout after the cart take-over, with the order")]
    public async Task LoggingInFromTheBuyerStepReturnsToItWithTheGuestsOrder()
    {
        // The browser suite shares one shop, so Anna's address carries a suffix of its own.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, Password, Password);
        await ClearCookiesAsync();
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync(DomainDrivenDesign);
        await product.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        await cart.ProceedToCheckoutAsync();
        var login = await LoginPage.NavigateToAsync(Page, "/checkout/buyer");

        await login.LoginAsync(email, Password);

        Assert.Equal("/checkout/buyer", await CartTakeOverPage.Of(Page).FollowedPathAsync());
        var buyer = await BuyerInfoPage.OpenAsync(Page);
        Assert.Equal([$"{DomainDrivenDesign} x1"], await buyer.SummaryItemsAsync());
    }
}