using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class PaymentPage : BasePage
{
    private const string UrlPattern = "/checkout/payment";
    private const string ContinueButton = "payment-continue-button";
    private const string ProviderRadio = "payment-provider-radio";
    private const string Total = "order-summary-total";

    private PaymentPage(IPage page) : base(page)
    {
    }

    public static async Task<PaymentPage> OpenAsync(IPage page)
    {
        var payment = new PaymentPage(page);
        await payment.WaitForUrlAsync(UrlPattern);
        return payment;
    }

    /// <summary>The payment step of the shop at <paramref name="shopUrl"/>, a shop other than the suite's own, once the browser shows it.</summary>
    public static async Task<PaymentPage> OpenAsync(IPage page, string shopUrl)
    {
        await page.WaitForURLAsync(shopUrl + UrlPattern);
        return new PaymentPage(page);
    }

    public async Task<PaymentPage> SelectFirstPaymentProviderAsync()
    {
        if (await ExistsAsync(ProviderRadio))
        {
            await SelectFirstRadioAsync(ProviderRadio);
        }

        return this;
    }

    /// <summary>The checkout session's total as the order summary shows it, e.g. <c>9.99 EUR</c>.</summary>
    public Task<string> TotalAsync() => Page.Locator($"[data-test='{Total}']").InnerTextAsync();

    public async Task<ReviewPage> ContinueToReviewAsync()
    {
        await ClickAsync(ContinueButton);
        return await ReviewPage.OpenAsync(Page);
    }

    /// <summary>
    /// Chooses the one way to pay the shop offers — its name depends on the provider the shop is wired to (the mock,
    /// or the provider stub the embedded suite starts), so the choice is made by being the only one.
    /// </summary>
    public async Task<PaymentPage> ChooseTheOfferedPaymentProviderAsync()
    {
        var radio = Page.Locator($"[data-test='{ProviderRadio}']");
        await Assertions.Expect(radio).ToHaveCountAsync(1);
        await radio.CheckAsync();
        return this;
    }

    /// <summary>Submits the form and answers the path of whatever page the shop opens next, without expecting one.</summary>
    public async Task<string> SubmitAndShowNextPathAsync()
    {
        var button = Page.Locator($"[data-test='{ContinueButton}']");
        await Assertions.Expect(button).ToBeVisibleAsync();
        await Page.RunAndWaitForRequestFinishedAsync(
            () => button.ClickAsync(),
            new PageRunAndWaitForRequestFinishedOptions { Predicate = request => request.IsNavigationRequest && request.Method == "GET" });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        return CurrentPath;
    }
}