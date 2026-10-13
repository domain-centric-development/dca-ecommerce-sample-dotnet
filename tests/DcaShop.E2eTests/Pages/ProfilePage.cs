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

    private ILocator Input(string dataTest) => Page.Locator($"[data-test='{dataTest}']");
}