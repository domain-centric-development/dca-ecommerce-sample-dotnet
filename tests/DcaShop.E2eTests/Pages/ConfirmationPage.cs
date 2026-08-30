using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class ConfirmationPage : BasePage
{
    private const string UrlPattern = "/checkout/confirmation";
    private const string ConfirmationMessage = "confirmation-message";

    private ConfirmationPage(IPage page) : base(page)
    {
    }

    public static async Task<ConfirmationPage> OpenAsync(IPage page)
    {
        var confirmation = new ConfirmationPage(page);
        await confirmation.WaitForUrlAsync(UrlPattern);
        return confirmation;
    }

    public Task<bool> IsOrderConfirmedAsync() => ExistsAsync(ConfirmationMessage);
}
