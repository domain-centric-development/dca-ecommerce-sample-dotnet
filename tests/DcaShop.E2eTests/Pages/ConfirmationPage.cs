using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class ConfirmationPage : BasePage
{
    private const string UrlPattern = "/checkout/confirmation";
    private const string ConfirmationTitle = "confirmation-title";

    private ConfirmationPage(IPage page) : base(page)
    {
    }

    public static async Task<ConfirmationPage> OpenAsync(IPage page)
    {
        var confirmation = new ConfirmationPage(page);
        await confirmation.WaitForUrlAsync(UrlPattern);
        return confirmation;
    }

    /// <summary>The confirmation's title as the browser renders it.</summary>
    public async Task<string> TitleAsync() => (await Page.Locator($"[data-test='{ConfirmationTitle}']").InnerTextAsync()).Trim();

    /// <summary>Everything the confirmation says, as the browser renders it, once its title is shown.</summary>
    public async Task<string> TextAsync()
    {
        var title = Page.Locator($"[data-test='{ConfirmationTitle}']");
        await Assertions.Expect(title).ToBeVisibleAsync();
        return await Page.Locator(".confirmation").InnerTextAsync();
    }

    /// <summary>
    /// The total line of the confirmation's order summary, as its label and its amount. The line carries no data-test;
    /// it is read by its class.
    /// </summary>
    public async Task<(string Label, string Amount)> OrderTotalAsync()
    {
        var line = Page.Locator(".confirmation .order-summary__total");
        await Assertions.Expect(line).ToBeVisibleAsync();
        var parts = line.Locator("span");
        return ((await parts.Nth(0).InnerTextAsync()).Trim(), (await parts.Nth(1).InnerTextAsync()).Trim());
    }

    /// <summary>The order reference the confirmation names, without its label. The line carries no data-test; it is read by its class.</summary>
    public async Task<string> OrderReferenceAsync()
    {
        var line = Page.Locator(".confirmation__reference");
        await Assertions.Expect(line).ToBeVisibleAsync();
        return (await line.InnerTextAsync()).Replace("Order Reference:", string.Empty, StringComparison.Ordinal).Trim();
    }
}