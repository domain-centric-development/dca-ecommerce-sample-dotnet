using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>The backoffice login. Port of the Java sample's <c>BackofficeLoginPage</c>, same selectors.</summary>
public sealed class BackofficeLoginPage : BasePage
{
    private const string UrlPattern = "/backoffice/login**";
    private const string SubmitButton = "backoffice-login-submit";
    private const string LoginErrorMessage = "login-error-message";
    private const string LogoutMessage = "logout-message";

    private BackofficeLoginPage(IPage page) : base(page)
    {
    }

    public static async Task<BackofficeLoginPage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + "/backoffice/login");
        return await OpenAsync(page);
    }

    public static async Task<BackofficeLoginPage> OpenAsync(IPage page)
    {
        var login = new BackofficeLoginPage(page);
        await login.WaitForUrlAsync(UrlPattern);
        return login;
    }

    /// <summary>Creates the page object without waiting — for a page that may not have loaded yet.</summary>
    public static BackofficeLoginPage On(IPage page) => new(page);

    public async Task FillCredentialsAsync(string username, string password)
    {
        await FillAsync("username", username);
        await FillAsync("password", password);
    }

    /// <summary>Submits and expects the log — the credentials were right.</summary>
    public async Task<BackofficeEventsPage> SubmitAsync()
    {
        await ClickAsync(SubmitButton);
        await Page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.Ordinal));
        return await BackofficeEventsPage.OpenAsync(Page);
    }

    /// <summary>Submits and expects to stay here with an error — the credentials were wrong.</summary>
    public async Task SubmitExpectingErrorAsync()
    {
        await ClickAsync(SubmitButton);
        await WaitForAsync(LoginErrorMessage);
    }

    public Task<bool> ShowsLoginErrorAsync() => ExistsAsync(LoginErrorMessage);

    public Task<bool> ShowsLogoutMessageAsync() => ExistsAsync(LogoutMessage);
}
