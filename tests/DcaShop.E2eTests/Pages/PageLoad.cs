using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>
/// Waiting for the page the browser was sent to. Playwright's own wait for a URL also waits for Playwright to report
/// the page's load event, and after a form's redirect under the load of the parallel suites that report now and then
/// never comes, although the page has long finished loading. The wait therefore ends on the URL and then asks the page
/// itself whether it has loaded.
/// </summary>
public static class PageLoad
{
    private static readonly PageWaitForURLOptions OnCommit = new() { WaitUntil = WaitUntilState.Commit };
    private static readonly PageWaitForFunctionOptions EveryTenthOfASecond = new() { PollingInterval = 100 };

    /// <summary>Waits until the browser shows <paramref name="url"/> (a glob) and that page has loaded.</summary>
    public static async Task WaitForLoadedUrlAsync(this IPage page, string url)
    {
        await page.WaitForURLAsync(url, OnCommit);
        await LoadedAsync(page);
    }

    /// <summary>Waits until the browser shows a URL <paramref name="url"/> accepts and that page has loaded.</summary>
    public static async Task WaitForLoadedUrlAsync(this IPage page, Func<string, bool> url)
    {
        await page.WaitForURLAsync(url, OnCommit);
        await LoadedAsync(page);
    }

    private static Task LoadedAsync(IPage page) =>
        page.WaitForFunctionAsync("() => document.readyState === 'complete'", null, EveryTenthOfASecond);
}