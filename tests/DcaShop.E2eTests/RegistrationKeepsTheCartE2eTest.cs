using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest fills a cart and registers; the new account takes the cart over, no merge page between.</summary>
public sealed class RegistrationKeepsTheCartE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";
    private const string DomainDrivenDesign = "Domain-Driven Design";

    public RegistrationKeepsTheCartE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Registering keeps the guest cart")]
    public async Task TheCartAGuestFilledBecomesTheNewAccountsCart()
    {
        // The browser suite shares one shop, so Anna's address carries a suffix of its own.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync(DomainDrivenDesign);
        await product.AddToCartAsync();

        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, Password, Password);

        var cart = await CartPage.NavigateToAsync(Page);
        Assert.Equal([$"{DomainDrivenDesign} x1"], await cart.LineItemsAsync());
        Assert.Equal($"Welcome, {email}", await cart.HeaderGreetingAsync());
    }
}