using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest chooses how to pay and moves on to the review.</summary>
public sealed class ChooseHowToPayE2eTest : BaseE2eTest
{
    public ChooseHowToPayE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Choosing the provider opens the review")]
    public async Task ChoosingTheMockProviderOpensTheReviewStep()
    {
        var payment = await GuestOnThePaymentStepAsync();
        await payment.ChooseTheOfferedPaymentProviderAsync();

        var shownPath = await payment.SubmitAndShowNextPathAsync();

        Assert.Equal("/checkout/review", shownPath);
    }

    private async Task<PaymentPage> GuestOnThePaymentStepAsync()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");
        await product.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        var delivery = await (await buyer.FillBuyerInfoAsync("jane@example.com", "Jane", "Doe", "+49 30 1234567")).ContinueToDeliveryAsync();
        await delivery.FillAddressAsync("Hauptstrasse 1", "Berlin", "10115", "Germany", string.Empty);
        await delivery.ChooseShippingMethodAsync("Standard Shipping");
        return await delivery.ContinueToPaymentAsync();
    }
}