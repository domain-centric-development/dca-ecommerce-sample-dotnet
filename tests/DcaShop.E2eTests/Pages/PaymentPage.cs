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
        await page.WaitForLoadedUrlAsync(shopUrl + UrlPattern);
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

    /// <summary>
    /// Sends a "Place Order" to the shop from this page, with this page's form token — what a second tab or a stale
    /// page of the same browser would send — and answers the path of the page the shop shows in the end.
    /// </summary>
    public async Task<string> SendPlaceOrderAsync()
    {
        await Assertions.Expect(Page.Locator("[data-test='payment-form']")).ToBeVisibleAsync();
        await MarkDocumentAsync();
        await Page.EvaluateAsync("""
            () => {
              const token = document.querySelector("[data-test='payment-form'] input[name='__RequestVerificationToken']").value;
              const sent = document.createElement('form');
              sent.method = 'post';
              sent.action = '/checkout/confirm';
              const input = document.createElement('input');
              input.type = 'hidden';
              input.name = '__RequestVerificationToken';
              input.value = token;
              sent.appendChild(input);
              document.body.appendChild(sent);
              sent.submit();
            }
            """);
        // The shop's answer replaces the marked document, wherever its redirects lead
        await Page.WaitForFunctionAsync("() => window.__documentMark !== true && document.readyState !== 'loading'");
        return new Uri(Page.Url).AbsolutePath;
    }
}