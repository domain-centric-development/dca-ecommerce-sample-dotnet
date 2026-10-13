using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class ReviewPage : BasePage
{
    private const string UrlPattern = "/checkout/review";
    private const string PlaceOrderButton = "review-place-order-button";

    private ReviewPage(IPage page) : base(page)
    {
    }

    public static async Task<ReviewPage> OpenAsync(IPage page)
    {
        var review = new ReviewPage(page);
        await review.WaitForUrlAsync(UrlPattern);
        return review;
    }

    public bool IsOnPage => CurrentPath.StartsWith(UrlPattern, StringComparison.Ordinal);

    public Task<bool> ShowsEmailAsync(string email) => PageContainsAsync(email);

    public Task<bool> ShowsAddressAsync(string address) => PageContainsAsync(address);

    /// <summary>
    /// The steps of the progress bar on the shown page, each as "label state" — done, current or open. The steps
    /// carry no data-test; they are read by their place in the bar.
    /// </summary>
    public async Task<IReadOnlyList<string>> ProgressAsync()
    {
        var steps = Page.Locator("[data-test='checkout-progress'] .checkout__step");
        await Assertions.Expect(steps).ToHaveCountAsync(4);
        var shown = new List<string>();
        for (var i = 0; i < await steps.CountAsync(); i++)
        {
            var step = steps.Nth(i);
            var classes = await step.GetAttributeAsync("class") ?? string.Empty;
            var state = classes.Contains("checkout__step--completed", StringComparison.Ordinal) ? "done"
                : classes.Contains("checkout__step--active", StringComparison.Ordinal) ? "current" : "open";
            // The text, not the rendered one: the stylesheet sets the labels in capitals
            shown.Add($"{(await step.Locator(".checkout__step-label").TextContentAsync() ?? string.Empty).Trim()} {state}");
        }

        return shown;
    }

    public async Task<ConfirmationPage> PlaceOrderAsync()
    {
        await ClickAsync(PlaceOrderButton);
        return await ConfirmationPage.OpenAsync(Page);
    }

    /// <summary>Presses "Place Order" and answers the path of whatever page the shop opens next, without expecting one.</summary>
    public async Task<string> PlaceOrderAndShowNextPathAsync()
    {
        var button = Page.Locator($"[data-test='{PlaceOrderButton}']");
        await Assertions.Expect(button).ToBeVisibleAsync();
        await Page.RunAndWaitForRequestFinishedAsync(
            () => button.ClickAsync(),
            new PageRunAndWaitForRequestFinishedOptions { Predicate = request => request.IsNavigationRequest && request.Method == "GET" });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        return CurrentPath;
    }
}