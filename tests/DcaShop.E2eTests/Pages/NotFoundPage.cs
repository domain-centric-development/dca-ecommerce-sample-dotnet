using System.Text.RegularExpressions;

using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>The page the shop answers for a path that does not exist.</summary>
public sealed partial class NotFoundPage : BasePage
{
    private const string BrowseLink = "error-browse-link";
    private const string HomeLink = "error-home-link";

    private NotFoundPage(IPage page) : base(page)
    {
    }

    public static async Task<NotFoundPage> NavigateToAsync(IPage page, string path)
    {
        await page.GotoAsync(BaseUrl + path);
        var notFound = new NotFoundPage(page);
        await notFound.WaitForUrlAsync(path);
        await notFound.WaitForAsync(BrowseLink);
        return notFound;
    }

    /// <summary>The document title, as the browser tab shows it.</summary>
    public Task<string> DocumentTitleAsync() => Page.TitleAsync();

    /// <summary>True when the page shows the text, whitespace and line breaks read as single spaces.</summary>
    public async Task<bool> ShowsAsync(string text) =>
        Whitespace().Replace(await Page.Locator("body").InnerTextAsync(), " ").Contains(text, StringComparison.Ordinal);

    /// <summary>The label and target of the link to the catalogue.</summary>
    public Task<(string Label, string? Href)> BrowseLinkAsync() => LinkAsync(BrowseLink);

    /// <summary>The label and target of the link to the homepage.</summary>
    public Task<(string Label, string? Href)> HomeLinkAsync() => LinkAsync(HomeLink);

    private async Task<(string Label, string? Href)> LinkAsync(string dataTest)
    {
        var link = Page.Locator($"[data-test='{dataTest}']");
        return ((await link.InnerTextAsync()).Trim(), await link.GetAttributeAsync("href"));
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}