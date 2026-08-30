using Microsoft.Playwright;

namespace DcaShop.E2eTests.Pages;

public sealed class DeliveryPage : BasePage
{
    private const string UrlPattern = "/checkout/delivery";
    private const string ContinueButton = "delivery-continue-button";
    private const string BackLink = "delivery-back-link";
    private const string ShippingRadio = "delivery-shipping-radio";

    private DeliveryPage(IPage page) : base(page)
    {
    }

    public static async Task<DeliveryPage> OpenAsync(IPage page)
    {
        var delivery = new DeliveryPage(page);
        await delivery.WaitForUrlAsync(UrlPattern);
        return delivery;
    }

    public async Task<DeliveryPage> FillAddressAsync(string street, string city, string postalCode, string country, string state)
    {
        await FillAsync("street", street);
        await FillAsync("city", city);
        await FillAsync("postalCode", postalCode);
        await FillAsync("country", country);
        await FillAsync("state", state);
        return this;
    }

    public async Task<DeliveryPage> SelectFirstShippingOptionAsync()
    {
        if (await ExistsAsync(ShippingRadio))
        {
            await SelectFirstRadioAsync(ShippingRadio);
        }

        return this;
    }

    public async Task<PaymentPage> ContinueToPaymentAsync()
    {
        await ClickAsync(ContinueButton);
        return await PaymentPage.OpenAsync(Page);
    }

    public async Task<BuyerInfoPage> GoBackAsync()
    {
        await ClickAsync(BackLink);
        return await BuyerInfoPage.OpenAsync(Page);
    }

    public Task<bool> HasBackLinkAsync() => ExistsAsync(BackLink);
}
