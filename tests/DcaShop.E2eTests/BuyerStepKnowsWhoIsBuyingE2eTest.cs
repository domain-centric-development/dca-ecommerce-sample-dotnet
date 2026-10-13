using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest who registers after opening the buyer step finds the step greeting the account and its email filled in.</summary>
public sealed class BuyerStepKnowsWhoIsBuyingE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";
    private const string DomainDrivenDesign = "Domain-Driven Design";

    public BuyerStepKnowsWhoIsBuyingE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Logged in customer sees who is buying")]
    public async Task TheBuyerStepNamesTheLoggedInAccountAndFillsInItsEmail()
    {
        // The browser suite shares one shop, so Anna's address carries a suffix of its own.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync(DomainDrivenDesign);
        await product.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        await cart.ProceedToCheckoutAsync();
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, Password, Password);

        var buyer = await BuyerInfoPage.NavigateToAsync(Page);

        Assert.Equal($"Logged in as {email}", await buyer.LoggedInBannerTextAsync());
        Assert.False(await buyer.ShowsAuthOptionsAsync(), "A logged-in customer must not be offered a login or a registration");
        Assert.Equal(email, await buyer.EmailValueAsync());
    }
}