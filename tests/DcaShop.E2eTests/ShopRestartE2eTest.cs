using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// A logged-in customer comes back to a shop that was restarted and has forgotten every account. The restarted shop is
/// a second shop beside the suite's, started with the same configuration and therefore the same signing secret, but
/// knowing none of the accounts registered since. The browser keeps its cookies across the two, because cookies tell
/// hosts apart, not ports.
/// </summary>
public sealed class ShopRestartE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";

    public ShopRestartE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [RestartingFact(DisplayName = "Restart turns the customer into a visitor")]
    public async Task AfterARestartTheCatalogueOffersLoginAndRegisterInsteadOfTheGreeting()
    {
        // The browser suite shares one shop, so Anna's address carries a suffix of its own.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, Password, Password);
        var loggedIn = await ProductCatalogPage.NavigateToAsync(Page);
        Assert.Equal($"Welcome, {email}", await loggedIn.HeaderGreetingAsync());
        using var restarted = SideShop.Start();

        var catalogue = await ProductCatalogPage.NavigateToAsync(Page, restarted.BaseUrl);

        Assert.True(await catalogue.HasProductsAsync(), "the catalogue shows its products");
        Assert.False(await catalogue.ShowsErrorMessageAsync(), "the catalogue shows no error message");
        Assert.True(await catalogue.HeaderOffersLoginAsync(), "the header offers to log in");
        Assert.True(await catalogue.HeaderOffersRegisterAsync(), "the header offers to register");
        Assert.Equal(string.Empty, await catalogue.HeaderGreetingAsync());
    }
}