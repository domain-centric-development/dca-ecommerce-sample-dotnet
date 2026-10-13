using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A logged-in customer replaces their password and stays logged in.</summary>
public sealed class ChangePasswordE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";

    public ChangePasswordE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Changing the password")]
    public async Task AChangedPasswordIsConfirmedOnAnEmptyFormAndTheCustomerStaysLoggedIn()
    {
        // The browser suite shares one shop, so Anna's address carries a suffix of its own. Registering logs her in.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, Password, Password);
        var changePassword = await ChangePasswordPage.NavigateToAsync(Page);

        var answered = await changePassword.ChangeAsync(Password, "Better456", "Better456");

        Assert.Equal("/account/change-password", answered.ShownPath);
        Assert.Equal("Your password has been changed.", await answered.SuccessMessageAsync());
        Assert.Equal([string.Empty, string.Empty, string.Empty], await answered.PasswordFieldValuesAsync());
        Assert.Equal($"Welcome, {email}", await answered.HeaderGreetingAsync());
    }
}