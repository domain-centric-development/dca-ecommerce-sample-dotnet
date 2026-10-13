using System.Globalization;

using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A customer who has just logged in opens their account from the header.</summary>
public sealed class MyAccountE2eTest : BaseE2eTest
{
    private const string Password = "Secret123";

    public MyAccountE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Overview shows the account")]
    public async Task TheOverviewShowsTheAddressAndTheTimeOfThisLogin()
    {
        // The browser suite shares one shop, so Anna's address carries a suffix of its own.
        var email = $"anna.schmidt-{Guid.NewGuid():N}@example.com";
        var register = await RegisterPage.NavigateToAsync(Page);
        await register.RegisterAsync("Anna", "Schmidt", "1990-05-17", email, Password, Password);
        await ClearCookiesAsync();
        var login = await LoginPage.NavigateToAsync(Page);
        var before = DateTime.UtcNow;
        await login.LoginAsync(email, Password);
        var after = DateTime.UtcNow;
        var landing = await CartPage.OpenAsync(Page);

        var account = await landing.FollowMyAccountAsync();

        Assert.Equal("/account", account.ShownPath);
        Assert.Equal($"Welcome, {email}", await account.HeadingAsync());
        Assert.Contains("Account Overview", await account.CardTitlesAsync());
        Assert.Equal(email, await account.DetailAsync("Email"));

        // The login happened between the two readings of the clock; either minute is the one the page may show.
        Assert.Contains(await account.DetailAsync("Last Login"), new[] { Minute(before), Minute(after) });
    }

    private static string Minute(DateTime utc) => utc.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
}