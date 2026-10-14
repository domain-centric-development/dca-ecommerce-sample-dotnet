using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class RegisterPage : BasePage
{
    private const string UrlPattern = "/register**";
    private const string SubmitButton = "register-submit-button";
    private const string ErrorMessage = "register-error-message";

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

    /// <summary>Fills in every field of the form as given and submits it; waits until the browser has left the page.</summary>
    public async Task RegisterAsync(
        string firstName, string lastName, string dateOfBirth, string email, string password, string confirmPassword)
    {
        await WaitForAsync("register-form");
        await FillAsync("firstName", firstName);
        await FillAsync("lastName", lastName);
        await FillAsync("dateOfBirth", dateOfBirth);
        await FillAsync("email", email);
        await FillAsync("password", password);
        await FillAsync("confirmPassword", confirmPassword);
        await ClickAsync(SubmitButton);
        await Page.WaitForLoadedUrlAsync(url => !url.Contains("/register", StringComparison.Ordinal));
    }

    /// <summary>Fills in every field of the form as given and submits it; expects to stay here with the page's refusal.</summary>
    public async Task SubmitExpectingRefusalAsync(
        string firstName, string lastName, string dateOfBirth, string email, string password, string confirmPassword)
    {
        await WaitForAsync("register-form");
        await FillAsync("firstName", firstName);
        await FillAsync("lastName", lastName);
        await FillAsync("dateOfBirth", dateOfBirth);
        await FillAsync("email", email);
        await FillAsync("password", password);
        await FillAsync("confirmPassword", confirmPassword);
        await ClickAsync(SubmitButton);
        await WaitForAsync(ErrorMessage);
    }

    /// <summary>
    /// Fills in every field of the form as given and submits it, expecting the page to refuse it: answers the error
    /// message the page shows instead.
    /// </summary>
    public async Task<string> RegisterRefusedAsync(
        string firstName, string lastName, string dateOfBirth, string email, string password, string confirmPassword)
    {
        await SubmitExpectingRefusalAsync(firstName, lastName, dateOfBirth, email, password, confirmPassword);
        return await ErrorMessageAsync();
    }

    /// <summary>The refusal the page shows, as the browser renders it.</summary>
    public async Task<string> ErrorMessageAsync() =>
        (await Page.Locator($"[data-test='{ErrorMessage}']").InnerTextAsync()).Trim();

    /// <summary>What the input named <paramref name="name"/> holds right now.</summary>
    public Task<string> FieldValueAsync(string name) => Page.Locator($"input[name='{name}']").InputValueAsync();

    public async Task RegisterAsync(string email, string password)
    {
        await FillAsync("firstName", DefaultFirstName);
        await FillAsync("lastName", DefaultLastName);
        await FillAsync("dateOfBirth", DefaultDateOfBirth);
        await FillAsync("email", email);
        await FillAsync("password", password);
        await FillAsync("confirmPassword", password);
        await ClickAsync(SubmitButton);
        await Page.WaitForLoadedUrlAsync(url => !url.Contains("/register", StringComparison.Ordinal));
    }
}