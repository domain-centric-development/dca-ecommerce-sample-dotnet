using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A visitor creates an account on the registration page and is logged in right away.</summary>
public sealed class RegistrationE2eTest : BaseE2eTest
{
    public RegistrationE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Registration logs the visitor in")]
    public async Task ASuccessfulRegistrationLeadsHomeAndGreetsTheNewCustomer()
    {
        var register = await RegisterPage.NavigateToAsync(Page);

        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", "anna.schmidt@example.com", "Secret123", "Secret123");

        Assert.Equal("/", CurrentPath);
        var home = await HomePage.OpenAsync(Page);
        Assert.Equal("Welcome, anna.schmidt@example.com", await home.HeaderGreetingAsync());
        Assert.False(await home.HeaderOffersRegisterAsync(), "the header no longer offers to register");
    }
}