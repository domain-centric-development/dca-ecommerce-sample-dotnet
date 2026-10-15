using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest tells the checkout where the order goes and how fast.</summary>
public sealed class ChooseDeliveryAddressE2eTest : BaseE2eTest
{
    public ChooseDeliveryAddressE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Address and shipping open the payment step")]
    public async Task SubmittingAnAddressAndAShippingMethodOpensThePaymentStep()
    {
        var delivery = await GuestOnTheDeliveryStepAsync();
        await delivery.FillAddressAsync("Hauptstrasse 1", "Berlin", "10115", "Germany", string.Empty);
        await delivery.ChooseShippingMethodAsync("Standard Shipping");

        var shownPath = await delivery.SubmitAndShowNextPathAsync();

        Assert.Equal("/checkout/payment", shownPath);
    }

    [E2eFact(DisplayName = "A shipping price sent with the form is not taken")]
    public async Task TheShippingPriceIsTheChosenMethodsPriceWhateverTheFormCarries()
    {
        var delivery = await GuestOnTheDeliveryStepAsync();
        await delivery.FillAddressAsync("Hauptstrasse 1", "Berlin", "10115", "Germany", string.Empty);
        await delivery.ChooseShippingMethodAsync("Overnight Shipping");
        await delivery.SendShippingPriceAsync("0.00");

        var payment = await delivery.ContinueToPaymentAsync();

        Assert.Equal("54.99 EUR", await payment.SummaryLineAsync("Subtotal"));
        Assert.Equal("19.99 EUR", await payment.SummaryLineAsync("Shipping"));
        Assert.Equal("74.98 EUR", (await payment.TotalAsync()).Trim());
    }

    private async Task<DeliveryPage> GuestOnTheDeliveryStepAsync()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");
        await product.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        return await (await buyer.FillBuyerInfoAsync("jane@example.com", "Jane", "Doe", "+49 30 1234567")).ContinueToDeliveryAsync();
    }
}