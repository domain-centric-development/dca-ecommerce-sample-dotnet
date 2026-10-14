using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class LoginPage : BasePage
{
    private const string UrlPattern = "/login**";
    private const string SubmitButton = "login-submit-button";
    private const string RegisterLink = "login-register-link";
    private const string ErrorMessage = "login-error-message";
    private const string EmailInput = "login-email-input";
    private const string PasswordInput = "login-password-input";

    private LoginPage(IPage page) : base(page)
    {
    }

    public static async Task<LoginPage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + "/login");
        return await OpenAsync(page);
    }

    public static async Task<LoginPage> OpenAsync(IPage page)
    {
        var login = new LoginPage(page);
        await login.WaitForUrlAsync(UrlPattern);
        return login;
    }

    public async Task LoginAsync(string email, string password)
    {
        await FillAsync("email", email);
        await FillAsync("password", password);
        await ClickAsync(SubmitButton);

        // Login always leaves /login — either onto the merge page or straight back to the shop.
        await Page.WaitForLoadedUrlAsync(url => !url.Contains("/login", StringComparison.Ordinal));
    }

    /// <summary>Submits the form with credentials the shop refuses: the login page answers again, with its message.</summary>
    public async Task SubmitRefusedAsync(string email, string password)
    {
        await FillAsync("email", email);
        await FillAsync("password", password);
        await ClickAsync(SubmitButton);
        await WaitForAsync(ErrorMessage);
    }

    /// <summary>The error message the page shows, as the browser renders it.</summary>
    public async Task<string> ErrorMessageTextAsync() =>
        (await Page.Locator($"[data-test='{ErrorMessage}']").InnerTextAsync()).Trim();

    /// <summary>What the Email field holds.</summary>
    public Task<string> EmailValueAsync() => Page.Locator($"[data-test='{EmailInput}']").InputValueAsync();

    /// <summary>What the Password field holds.</summary>
    public Task<string> PasswordValueAsync() => Page.Locator($"[data-test='{PasswordInput}']").InputValueAsync();

    public async Task<RegisterPage> GoToRegisterAsync()
    {
        await ClickAsync(RegisterLink);
        return await RegisterPage.OpenAsync(Page);
    }

    public Task<bool> ShowsErrorAsync() => ExistsAsync(ErrorMessage);
}