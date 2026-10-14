using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>
/// Waiting for the page the browser was sent to. Playwright's own wait for a URL waits for Playwright to report the
/// page's load event once the URL matches, and after a form's redirect under the load of the parallel suites that
/// report now and then never comes, although the page has long finished loading. The wait therefore watches the URL
/// the browser shows and then asks the page itself whether it has loaded.
/// </summary>
public static class PageLoad
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    /// <summary>Waits until the browser shows <paramref name="url"/> (a glob) and that page has loaded.</summary>
    public static Task WaitForLoadedUrlAsync(this IPage page, string url)
    {
        var glob = GlobToRegex(url);
        return page.WaitForLoadedUrlAsync(shown => glob.IsMatch(shown), url);
    }

    /// <summary>Waits until the browser shows a URL <paramref name="url"/> accepts and that page has loaded.</summary>
    public static Task WaitForLoadedUrlAsync(this IPage page, Func<string, bool> url) =>
        page.WaitForLoadedUrlAsync(url, "the expected URL");

    private static async Task WaitForLoadedUrlAsync(this IPage page, Func<string, bool> url, string expected)
    {
        var deadline = DateTime.UtcNow + Patience;
        while (!url(page.Url))
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"The browser did not show {expected} within {Patience.TotalSeconds} seconds; it shows {page.Url}.");
            }

            await Task.Delay(50);
        }

        // Polled from here, not with a function the page polls: a browser that runs no script runs no timer of the
        // page's either, while an evaluation still answers.
        while (!await HasLoadedAsync(page))
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"The page at {page.Url} did not finish loading within {Patience.TotalSeconds} seconds.");
            }

            await Task.Delay(100);
        }
    }

    /// <summary>Whether the shown document has loaded; a page that sends itself on may replace it while it is asked.</summary>
    private static async Task<bool> HasLoadedAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<string>("() => document.readyState") == "complete";
        }
        catch (PlaywrightException e) when (e.Message.Contains("Execution context was destroyed", StringComparison.Ordinal))
        {
            return false;
        }
    }

    /// <summary>Playwright's URL glob: <c>**</c> matches anything, <c>*</c> anything but a slash.</summary>
    private static Regex GlobToRegex(string glob)
    {
        var pattern = new StringBuilder("^");
        for (var i = 0; i < glob.Length; i++)
        {
            if (glob[i] != '*')
            {
                pattern.Append(Regex.Escape(glob[i].ToString()));
            }
            else if (i + 1 < glob.Length && glob[i + 1] == '*')
            {
                pattern.Append(".*");
                i++;
            }
            else
            {
                pattern.Append("[^/]*");
            }
        }

        return new Regex(pattern.Append('$').ToString(), RegexOptions.CultureInvariant);
    }
}