using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class RegisterPage : BasePage
{
    private const string UrlPattern = "/register**";
    private const string SubmitButton = "register-submit-button";

    /// <summary>Registration needs an owner; flows that only care about credentials use these.</summary>
    private const string DefaultFirstName = "Test";
    private const string DefaultLastName = "User";
    private const string DefaultDateOfBirth = "1990-05-17";

    private RegisterPage(IPage page) : base(page)
    {
    }

    public static async Task<RegisterPage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + "/register");
        return await OpenAsync(page);
    }

    public static async Task<RegisterPage> OpenAsync(IPage page)
    {
        var register = new RegisterPage(page);
        await register.WaitForUrlAsync(UrlPattern);
        return register;
    }

    public async Task RegisterAsync(string email, string password)
    {
        await FillAsync("firstName", DefaultFirstName);
        await FillAsync("lastName", DefaultLastName);
        await FillAsync("dateOfBirth", DefaultDateOfBirth);
        await FillAsync("email", email);
        await FillAsync("password", password);
        await FillAsync("confirmPassword", password);
        await ClickAsync(SubmitButton);
        await Page.WaitForURLAsync(url => !url.Contains("/register", StringComparison.Ordinal));
    }
}
