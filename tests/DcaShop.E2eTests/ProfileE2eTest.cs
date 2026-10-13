using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A customer changes their address on "My Profile", and from then on only the new one is their login.</summary>
public sealed class ProfileE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";

    public ProfileE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Saving the profile")]
    public async Task SavedChangesAreConfirmedOnTheProfileAndTheHeaderGreetsTheNewAddress()
    {
        // The browser suite shares one shop, so both of Anna's addresses carry a suffix of their own.
        var run = Guid.NewGuid().ToString("N");
        var oldEmail = $"anna.schmidt-{run}@example.com";
        var newEmail = $"anna.meyer-{run}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", oldEmail, Password, Password);
        var profile = await ProfilePage.NavigateToAsync(Page);

        var saved = await (await profile.FillDetailsAsync(newEmail, "1990-05-18")).SaveExpectingSuccessAsync();

        Assert.Equal("/account/profile", saved.ShownPath);
        Assert.Equal("Your profile has been updated.", await saved.SuccessMessageAsync());
        Assert.Equal(newEmail, await saved.EmailAsync());
        Assert.Equal("1990-05-18", await saved.DateOfBirthAsync());
        Assert.Equal($"Welcome, {newEmail}", await saved.HeaderGreetingAsync());
    }

    [E2eFact(DisplayName = "Old address no longer logs in")]
    public async Task AnAddressGivenUpOnTheProfileIsRefusedAtTheLogin()
    {
        // The browser suite shares one shop, so both of Anna's addresses carry a suffix of their own.
        var run = Guid.NewGuid().ToString("N");
        var oldEmail = $"anna.schmidt-{run}@example.com";
        var newEmail = $"anna.meyer-{run}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", oldEmail, Password, Password);
        var profile = await ProfilePage.NavigateToAsync(Page);
        await (await profile.FillDetailsAsync(newEmail, "1990-05-17")).SaveExpectingSuccessAsync();
        await ClearCookiesAsync();
        var login = await LoginPage.NavigateToAsync(Page);

        await login.SubmitRefusedAsync(oldEmail, Password);

        Assert.Equal("Invalid email or password", await login.ErrorMessageTextAsync());
    }
}