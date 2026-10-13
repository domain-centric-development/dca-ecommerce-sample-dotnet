using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>The change-password page at <c>/account/change-password</c>.</summary>
public sealed class ChangePasswordPage : BasePage
{
    private const string Path = "/account/change-password";
    private const string Form = "change-password-form";
    private const string SubmitButton = "change-password-submit-button";
    private const string SuccessMessage = "change-password-success-message";

    private static readonly string[] PasswordFields = ["currentPassword", "newPassword", "confirmPassword"];

    private ChangePasswordPage(IPage page) : base(page)
    {
    }

    public static async Task<ChangePasswordPage> NavigateToAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl + Path);
        return await OpenAsync(page);
    }

    public static async Task<ChangePasswordPage> OpenAsync(IPage page)
    {
        var changePassword = new ChangePasswordPage(page);
        await changePassword.WaitForUrlAsync(Path);
        await changePassword.WaitForAsync(Form);
        return changePassword;
    }

    public string ShownPath => CurrentPath;

    /// <summary>Fills in the three password fields and submits them; waits for the page that answers with its message.</summary>
    public async Task<ChangePasswordPage> ChangeAsync(string currentPassword, string newPassword, string confirmPassword)
    {
        await Assertions.Expect(Page.Locator($"[data-test='{Form}']")).ToBeVisibleAsync();
        await FillAsync("currentPassword", currentPassword);
        await FillAsync("newPassword", newPassword);
        await FillAsync("confirmPassword", confirmPassword);
        await ClickAsync(SubmitButton);
        await WaitForAsync(SuccessMessage);
        return await OpenAsync(Page);
    }

    /// <summary>The success message the page shows, as the browser renders it.</summary>
    public async Task<string> SuccessMessageAsync() =>
        (await Page.Locator($"[data-test='{SuccessMessage}']").InnerTextAsync()).Trim();

    /// <summary>What the current, the new and the confirmed password fields hold, in that order.</summary>
    public async Task<IReadOnlyList<string>> PasswordFieldValuesAsync()
    {
        var values = new List<string>();
        foreach (var name in PasswordFields)
        {
            values.Add(await Page.Locator($"input[name='{name}']").InputValueAsync());
        }

        return values;
    }
}