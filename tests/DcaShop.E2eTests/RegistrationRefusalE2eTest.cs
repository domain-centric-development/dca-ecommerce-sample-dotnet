using System.Globalization;

using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A visitor tries to register an address another account already has, and the shop refuses it.</summary>
public sealed class RegistrationRefusalE2eTest : BaseE2eTest
{
    public RegistrationRefusalE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Taken address is refused")]
    public async Task AnAddressAnotherAccountHasIsRefusedAndTheVisitorStaysLoggedOut()
    {
        // The shop is shared by the whole run, so the earlier account takes an address of its own.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var earlier = await RegisterPage.NavigateToAsync(Page);
        await earlier.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, "Secret123", "Secret123");
        await ClearCookiesAsync();

        var register = await RegisterPage.NavigateToAsync(Page);
        var error = await register.RegisterRefusedAsync("Anna", "Schmidt", "1990-05-17", email, "Secret123", "Secret123");

        Assert.Equal("This email address is already registered", error);
        Assert.Equal(string.Empty, await register.HeaderGreetingAsync());
        Assert.True(await register.HeaderOffersRegisterAsync(), "the header still offers to register");
    }

    [E2eFact(DisplayName = "Todays date of birth is refused")]
    public async Task TodayAsDateOfBirthIsRefused()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var register = await RegisterPage.NavigateToAsync(Page);
        var error = await register.RegisterRefusedAsync(
            "Anna", "Schmidt", today, $"anna.today-{Guid.NewGuid():N}@example.com", "Secret123", "Secret123");

        Assert.Equal("Please check the email address, your name and your date of birth.", error);
    }
}