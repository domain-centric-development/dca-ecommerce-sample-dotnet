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

    /// <summary>The delivery step of the shop at <paramref name="shopUrl"/>, a shop other than the suite's own, once the browser shows it.</summary>
    public static async Task<DeliveryPage> OpenAsync(IPage page, string shopUrl)
    {
        await page.WaitForLoadedUrlAsync(shopUrl + UrlPattern);
        return new DeliveryPage(page);
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

    /// <summary>Chooses the shipping method shown under the given name.</summary>
    public async Task<DeliveryPage> ChooseShippingMethodAsync(string name)
    {
        var option = Page.Locator("[data-test='delivery-shipping-option']").Filter(new LocatorFilterOptions { HasText = name });
        await Assertions.Expect(option).ToBeVisibleAsync();
        await option.Locator($"[data-test='{ShippingRadio}']").CheckAsync();
        return this;
    }

    /// <summary>
    /// Changes the shipping price the form sends along with the chosen method to <paramref name="amount"/> — what a
    /// browser could send instead of the price the step shows.
    /// </summary>
    public async Task<DeliveryPage> SendShippingPriceAsync(string amount)
    {
        var price = Page.Locator($"[data-test='delivery-shipping-option']:has([data-test='{ShippingRadio}']:checked) input[name='shippingCost']");
        await Assertions.Expect(price).ToBeEnabledAsync();
        await price.EvaluateAsync("(field, value) => { field.value = value; }", amount);
        return this;
    }

    /// <summary>Submits the form and answers the path of whatever page the shop opens next, without expecting one.</summary>
    public async Task<string> SubmitAndShowNextPathAsync()
    {
        var button = Page.Locator($"[data-test='{ContinueButton}']");
        await Assertions.Expect(button).ToBeVisibleAsync();
        await Page.RunAndWaitForNextPageAsync(() => button.ClickAsync());
        return CurrentPath;
    }
}