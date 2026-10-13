using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>The account overview at <c>/account</c>.</summary>
public sealed class AccountPage : BasePage
{
    private const string UrlPattern = "/account";
    private const string Overview = "account-overview";
    private const string Greeting = "account-greeting";

    private AccountPage(IPage page) : base(page)
    {
    }

    public static async Task<AccountPage> OpenAsync(IPage page)
    {
        var account = new AccountPage(page);
        await account.WaitForUrlAsync(UrlPattern);
        await account.WaitForAsync(Overview);
        return account;
    }

    public string ShownPath => CurrentPath;

    /// <summary>The page heading as the browser renders it.</summary>
    public async Task<string> HeadingAsync() => (await Page.Locator($"[data-test='{Greeting}']").InnerTextAsync()).Trim();

    /// <summary>The titles of the cards the overview shows, in display order.</summary>
    public async Task<IReadOnlyList<string>> CardTitlesAsync() =>
        [.. (await Page.Locator($"[data-test='{Overview}'] .card__header-title").AllInnerTextsAsync()).Select(t => t.Trim())];

    /// <summary>The value shown beside the detail labelled <paramref name="label"/>; fails when no such detail is shown.</summary>
    public async Task<string> DetailAsync(string label)
    {
        var rows = Page.Locator($"[data-test='{Overview}'] .account__detail-row");
        for (var i = 0; i < await rows.CountAsync(); i++)
        {
            var row = rows.Nth(i);
            if ((await row.Locator(".account__detail-label").InnerTextAsync()).Trim() == label)
            {
                return (await row.Locator(".account__detail-value").InnerTextAsync()).Trim();
            }
        }

        throw new InvalidOperationException($"the overview shows no detail labelled \"{label}\"");
    }
}