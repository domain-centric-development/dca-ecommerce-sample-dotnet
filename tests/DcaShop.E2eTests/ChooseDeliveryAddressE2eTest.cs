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