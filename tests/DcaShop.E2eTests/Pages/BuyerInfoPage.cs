using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class BuyerInfoPage : BasePage
{
    private const string UrlPattern = "/checkout/buyer";
    private const string ContinueButton = "buyer-continue-button";
    private const string ErrorMessage = "buyer-error-message";
    private const string AuthOptions = "buyer-auth-options";
    private const string LoggedInBanner = "buyer-logged-in";
    private const string EmailInput = "buyer-email-input";

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

    /// <summary>The login/register prompt is offered to anonymous visitors only.</summary>
    public Task<bool> ShowsAuthOptionsAsync() => ExistsAsync(AuthOptions);

    /// <summary>The "Logged in as …" banner shown instead of the login/register prompt; empty when anonymous.</summary>
    public async Task<string> LoggedInBannerTextAsync() =>
        await ExistsAsync(LoggedInBanner)
            ? (await Page.Locator($"[data-test='{LoggedInBanner}']").TextContentAsync() ?? string.Empty).Trim()
            : string.Empty;

    /// <summary>The current value of the email input — prefilled from the identity for a registered visitor.</summary>
    public Task<string> EmailValueAsync() => Page.Locator($"[data-test='{EmailInput}']").InputValueAsync();

    public bool IsOnPage => CurrentPath.Contains("/checkout/buyer", StringComparison.Ordinal);
}
