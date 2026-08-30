using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class PaymentPage : BasePage
{
    private const string UrlPattern = "/checkout/payment";
    private const string ContinueButton = "payment-continue-button";
    private const string ProviderRadio = "payment-provider-radio";

    private PaymentPage(IPage page) : base(page)
    {
    }

    public static async Task<PaymentPage> OpenAsync(IPage page)
    {
        var payment = new PaymentPage(page);
        await payment.WaitForUrlAsync(UrlPattern);
        return payment;
    }

    public async Task<PaymentPage> SelectFirstPaymentProviderAsync()
    {
        if (await ExistsAsync(ProviderRadio))
        {
            await SelectFirstRadioAsync(ProviderRadio);
        }

        return this;
    }

    public async Task<ReviewPage> ContinueToReviewAsync()
    {
        await ClickAsync(ContinueButton);
        return await ReviewPage.OpenAsync(Page);
    }
}
