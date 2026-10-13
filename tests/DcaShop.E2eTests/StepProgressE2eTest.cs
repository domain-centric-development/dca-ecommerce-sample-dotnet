using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>The checkout's progress bar shows which steps are done and which one the guest is on.</summary>
public sealed class StepProgressE2eTest : BaseE2eTest
{
    public StepProgressE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Progress bar on the review")]
    public async Task TheReviewMarksTheFirstThreeStepsDoneAndTheReviewCurrent()
    {
        var payment = await GuestWhoSubmittedBuyerAndDeliveryAsync();
        await payment.ChooseTheOfferedPaymentProviderAsync();

        var review = await payment.ContinueToReviewAsync();

        Assert.Equal(new[] { "Buyer Info done", "Delivery done", "Payment done", "Review current" }, await review.ProgressAsync());
    }

    private async Task<PaymentPage> GuestWhoSubmittedBuyerAndDeliveryAsync()
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