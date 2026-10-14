using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>A guest places the order from the review and sees the confirmation.</summary>
public sealed class PlaceTheOrderE2eTest : BaseE2eTest
{
    public PlaceTheOrderE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Guest places the order")]
    public async Task PlacingTheOrderOpensTheConfirmation()
    {
        var review = await GuestOnTheReviewStepAsync();

        var shownPath = await review.PlaceOrderAndShowNextPathAsync();

        Assert.Equal("/checkout/confirmation", shownPath);
        var confirmation = await ConfirmationPage.OpenAsync(Page);
        Assert.Equal("Your order has been placed successfully", await confirmation.TitleAsync());
    }

    private async Task<ReviewPage> GuestOnTheReviewStepAsync()
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
        return await payment.ContinueToReviewAsync();
    }
}