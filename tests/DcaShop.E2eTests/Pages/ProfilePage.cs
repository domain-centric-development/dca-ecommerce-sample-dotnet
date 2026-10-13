using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>"My Profile" at <c>/account/profile</c>, the account page that changes the email address and the date of birth.</summary>
public sealed class ProfilePage : BasePage
{
    private const string Path = "/account/profile";
    private const string Profile = "profile-page";
    private const string EmailInput = "profile-email-input";
    private const string DateOfBirthInput = "profile-date-of-birth-input";
    private const string SubmitButton = "profile-submit-button";
    private const string SuccessMessage = "profile-success-message";
    private const string ErrorMessage = "profile-error-message";

    private ProfilePage(IPage page) : base(page)
    {
    }

    public static async Task<ProfilePage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + Path);
        return await OpenAsync(page);
    }

    public static async Task<ProfilePage> OpenAsync(IPage page)
    {
        var profile = new ProfilePage(page);
        await profile.WaitForUrlAsync(Path);
        await profile.WaitForAsync(Profile);
        return profile;
    }

    public string ShownPath => CurrentPath;

    /// <summary>Fills both fields of the form; the date of birth as yyyy-MM-dd.</summary>
    public async Task<ProfilePage> FillDetailsAsync(string email, string dateOfBirth)
    {
        await Assertions.Expect(Input(EmailInput)).ToBeVisibleAsync();
        await Input(EmailInput).FillAsync(email);
        await Input(DateOfBirthInput).FillAsync(dateOfBirth);
        return this;
    }

    /// <summary>Presses "Save Changes" and waits for the page to come back with its success message.</summary>
    public async Task<ProfilePage> SaveExpectingSuccessAsync()
    {
        await ClickAsync(SubmitButton);
        await WaitForAsync(SuccessMessage);
        return await OpenAsync(Page);
    }

    /// <summary>Presses "Save Changes" and waits for the page to come back with its refusal.</summary>
    public async Task<ProfilePage> SaveExpectingRefusalAsync()
    {
        await ClickAsync(SubmitButton);
        await WaitForAsync(ErrorMessage);
        return await OpenAsync(Page);
    }

    public async Task<string> SuccessMessageAsync() =>
        (await Page.Locator($"[data-test='{SuccessMessage}']").InnerTextAsync()).Trim();

    /// <summary>The refusal the page shows, as the browser renders it.</summary>
    public async Task<string> ErrorMessageAsync() =>
        (await Page.Locator($"[data-test='{ErrorMessage}']").InnerTextAsync()).Trim();

    /// <summary>What the email field holds, as the browser has it.</summary>
    public Task<string> EmailAsync() => Input(EmailInput).InputValueAsync();

    /// <summary>What the date-of-birth field holds, as yyyy-MM-dd.</summary>
    public Task<string> DateOfBirthAsync() => Input(DateOfBirthInput).InputValueAsync();

    private ILocator Input(string dataTest) => Page.Locator($"[data-test='{dataTest}']");
}