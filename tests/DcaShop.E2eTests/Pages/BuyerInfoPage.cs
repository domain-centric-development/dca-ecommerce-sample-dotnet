using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class BuyerInfoPage : BasePage
{
    private const string UrlPattern = "/checkout/buyer";
    private const string ContinueButton = "buyer-continue-button";
    private const string ErrorMessage = "buyer-error-message";

    private BuyerInfoPage(IPage page) : base(page)
    {
    }

    public static async Task<BuyerInfoPage> OpenAsync(IPage page)
    {
        var buyer = new BuyerInfoPage(page);
        await buyer.WaitForUrlAsync(UrlPattern);
        return buyer;
    }

    public async Task<BuyerInfoPage> FillBuyerInfoAsync(string email, string firstName, string lastName, string phone)
    {
        await FillAsync("email", email);
        await FillAsync("firstName", firstName);
        await FillAsync("lastName", lastName);
        await FillAsync("phone", phone);
        return this;
    }

    public async Task<DeliveryPage> ContinueToDeliveryAsync()
    {
        await ClickAsync(ContinueButton);
        return await DeliveryPage.OpenAsync(Page);
    }

    public async Task<BuyerInfoPage> SubmitWithErrorsAsync()
    {
        await ClickAsync(ContinueButton);
        // Brief wait: HTML5 validation prevents submission (no navigation),
        // server-side validation re-renders this page.
        await Page.WaitForTimeoutAsync(500);
        return this;
    }

    public async Task<bool> HasValidationErrorsAsync() =>
        await ExistsAsync(ErrorMessage)
        || await PageContainsAsync("valid email")
        || await PageContainsAsync("error")
        || await HasHtml5ValidationErrorsAsync();

    private async Task<bool> HasHtml5ValidationErrorsAsync() =>
        await Page.EvaluateAsync<bool>("() => { const form = document.querySelector('[data-test=\"buyer-form\"]'); return form != null && !form.checkValidity(); }");

    public bool IsOnPage => CurrentPath.Contains("/checkout/buyer", StringComparison.Ordinal);
}
