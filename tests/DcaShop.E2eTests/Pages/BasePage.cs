using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

/// <summary>Page Object base: <c>data-test</c> selectors only, same vocabulary as the Java sample's page objects.</summary>
public abstract class BasePage
{
    protected static string BaseUrl => BrowserFixture.BaseUrl;

    protected BasePage(IPage page)
    {
        Page = page;
    }

    protected IPage Page { get; }

    protected Task WaitForUrlAsync(string pattern) => Page.WaitForURLAsync(BaseUrl + pattern);

    protected Task ClickAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").ClickAsync();

    protected Task ClickFirstAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").First.ClickAsync();

    protected Task FillAsync(string name, string value) => Page.Locator($"input[name='{name}']").FillAsync(value);

    protected Task WaitForAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").First.WaitForAsync();

    protected async Task<bool> ExistsAsync(string dataTest) => await Page.Locator($"[data-test='{dataTest}']").CountAsync() > 0;

    protected Task SelectFirstRadioAsync(string dataTest) => Page.Locator($"[data-test='{dataTest}']").First.CheckAsync();

    protected string CurrentPath => Page.Url.Replace(BaseUrl, string.Empty, StringComparison.Ordinal);

    protected async Task<bool> PageContainsAsync(string text) => (await Page.Locator("body").TextContentAsync() ?? string.Empty).Contains(text, StringComparison.Ordinal);
}
