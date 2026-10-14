using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class ReviewPage : BasePage
{
    private const string UrlPattern = "/checkout/review";
    private const string PlaceOrderButton = "review-place-order-button";
    private const string ErrorMessage = "review-error-message";

    private ReviewPage(IPage page) : base(page)
    {
    }

    public static async Task<ReviewPage> OpenAsync(IPage page)
    {
        var review = new ReviewPage(page);
        await review.WaitForUrlAsync(UrlPattern);
        return review;
    }

    /// <summary>The review of the shop at <paramref name="shopUrl"/>, a shop other than the suite's own, once the browser shows it.</summary>
    public static async Task<ReviewPage> OpenAsync(IPage page, string shopUrl)
    {
        await page.WaitForURLAsync(shopUrl + UrlPattern);
        return new ReviewPage(page);
    }

    /// <summary>Presses "Place Order" and waits until the shop has answered it, wherever that answer leads.</summary>
    public Task PlaceOrderAndAwaitTheAnswerAsync() =>
        PressPlaceOrderAsync(button => Page.RunAndWaitForResponseAsync(
            () => button.ClickAsync(),
            response => response.Request.Method == "POST" && new Uri(response.Url).AbsolutePath == "/checkout/confirm"));

    /// <summary>The error the review shows, as written.</summary>
    public Task<string> ErrorMessageAsync() => VisibleTextAsync(ErrorMessage);

    /// <summary>Whether the page shows the confirmation of a placed order.</summary>
    public async Task<bool> ShowsConfirmationAsync() => await Page.Locator("[data-test='confirmation-title']").CountAsync() > 0;

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
        await PressPlaceOrderAsync(button => Page.RunAndWaitForRequestFinishedAsync(
            () => button.ClickAsync(),
            new PageRunAndWaitForRequestFinishedOptions { Predicate = request => request.IsNavigationRequest && request.Method == "GET" }));
        return CurrentPath;
    }

    /// <summary>
    /// Presses "Place Order" once it shows, through <paramref name="clickAndWait"/> — the click and the wait for what the
    /// caller needs to have happened — and then lets the page that is shown load.
    /// </summary>
    private async Task PressPlaceOrderAsync(Func<ILocator, Task> clickAndWait)
    {
        var button = Page.Locator($"[data-test='{PlaceOrderButton}']");
        await Assertions.Expect(button).ToBeVisibleAsync();
        await clickAndWait(button);
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }
}