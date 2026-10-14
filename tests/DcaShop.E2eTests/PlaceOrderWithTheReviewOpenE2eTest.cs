using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// A "Place Order" is accepted only from the review: sent from another step page while the review is open, it leads the
/// guest to the review, which asks them to look at the order first, and places nothing.
/// </summary>
public sealed class PlaceOrderWithTheReviewOpenE2eTest : BaseE2eTest
{
    public PlaceOrderWithTheReviewOpenE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Place order with the review open asks for the review")]
    public async Task APlaceOrderSentFromThePaymentStepEndsOnTheReviewWhichAsksToReviewTheOrder()
    {
        var payment = await GuestWithTheReviewOpenBackOnThePaymentStepAsync();

        var shownPath = await payment.SendPlaceOrderAsync();

        Assert.Equal("/checkout/review", shownPath);
        var review = await ReviewPage.OpenAsync(Page);
        Assert.Equal("Please review your order before placing it.", await review.ErrorMessageAsync());
        Assert.False(await review.ShowsConfirmationAsync());
        await NavigateToAsync("/checkout/confirmation");
        var cart = await CartPage.OpenAsync(Page);
        Assert.Equal("/cart", cart.ShownPath);
        Assert.Equal("No confirmed order found", await cart.ErrorMessageAsync());
    }

    private async Task<PaymentPage> GuestWithTheReviewOpenBackOnThePaymentStepAsync()
    {
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var product = await catalog.ViewProductAsync("Domain-Driven Design");
        await product.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        var delivery = await (await buyer.FillBuyerInfoAsync("jane@example.com", "Jane", "Doe", "+49 30 1234567")).ContinueToDeliveryAsync();
        await delivery.FillAddressAsync("Hauptstrasse 1", "Berlin", "10115", "Germany", string.Empty);
        await delivery.ChooseShippingMethodAsync("Standard Shipping");
        var payment = await delivery.ContinueToPaymentAsync();
        await payment.ChooseTheOfferedPaymentProviderAsync();
        await payment.ContinueToReviewAsync();
        await NavigateToAsync("/checkout/payment");
        return await PaymentPage.OpenAsync(Page);
    }
}