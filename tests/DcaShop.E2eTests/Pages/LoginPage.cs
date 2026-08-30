using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class LoginPage : BasePage
{
    private const string UrlPattern = "/login**";
    private const string SubmitButton = "login-submit-button";
    private const string RegisterLink = "login-register-link";
    private const string ErrorMessage = "login-error-message";

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
        await Page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.Ordinal));
    }

    public async Task<RegisterPage> GoToRegisterAsync()
    {
        await ClickAsync(RegisterLink);
        return await RegisterPage.OpenAsync(Page);
    }

    public Task<bool> ShowsErrorAsync() => ExistsAsync(ErrorMessage);
}
