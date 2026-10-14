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

    [E2eFact(DisplayName = "Registration says the account was created")]
    public async Task TheHomePageARegistrationLeadsToSaysTheAccountWasCreated()
    {
        // The browser suite shares one shop, and the test above registers Anna's plain address already.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);

        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, "Secret123", "Secret123");

        Assert.Equal("/", CurrentPath);
        var home = await HomePage.OpenAsync(Page);
        Assert.Equal(["Account created successfully! Welcome!"], await home.NoticesAsync());
    }

    [E2eFact(DisplayName = "Differing passwords are refused")]
    public async Task DifferingPasswordsAreRefusedAndKeepEverythingButThePasswords()
    {
        var register = await RegisterPage.NavigateToAsync(Page);

        await register.SubmitExpectingRefusalAsync(
            "Anna", "Schmidt", "1990-05-17", "anna.schmidt@example.com", "Secret123", "Secret124");

        Assert.Equal("Passwords do not match", await register.ErrorMessageAsync());
        Assert.Equal("Anna", await register.FieldValueAsync("firstName"));
        Assert.Equal("Schmidt", await register.FieldValueAsync("lastName"));
        Assert.Equal("1990-05-17", await register.FieldValueAsync("dateOfBirth"));
        Assert.Equal("anna.schmidt@example.com", await register.FieldValueAsync("email"));
        Assert.Equal(string.Empty, await register.FieldValueAsync("password"));
        Assert.Equal(string.Empty, await register.FieldValueAsync("confirmPassword"));
        Assert.Equal(string.Empty, await register.HeaderGreetingAsync());
        Assert.True(await register.HeaderOffersRegisterAsync(), "the header still offers to register");
    }
}