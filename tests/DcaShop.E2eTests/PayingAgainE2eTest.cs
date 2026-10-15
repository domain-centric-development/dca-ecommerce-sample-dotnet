using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// Paying again for a checkout whose payment the provider authorized. The requests are told from the other tests'
/// payments by the checkout reference, so the test shares the suite's payment provider stub and runs beside the others.
/// </summary>
public sealed class PayingAgainE2eTest : BaseE2eTest
{
    public PayingAgainE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Paying again for the same amount keeps the payment")]
    public async Task PayingAgainForTheSameAmountKeepsTheAuthorizedPayment()
    {
        // Given a checkout session whose payment of its total the payment provider authorized
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var detail = await catalog.ViewProductAsync("Domain-Driven Design");
        await detail.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        var delivery = await (await buyer.FillBuyerInfoAsync("payer@example.com", "Test", "Payer", "+1-555-0100")).ContinueToDeliveryAsync();
        await delivery.FillAddressAsync("123 Main Street", "Springfield", "12345", "United States", "IL");
        await delivery.ChooseShippingMethodAsync("Standard Shipping");
        var payment = await delivery.ContinueToPaymentAsync();
        await payment.ChooseTheOfferedPaymentProviderAsync();
        var review = await payment.ContinueToReviewAsync();

        // And the customer went back from the review to the payment step
        var paymentAgain = await review.BackToPaymentAsync();

        // When the customer pays through the payment provider again
        await paymentAgain.ChooseTheOfferedPaymentProviderAsync();
        var reviewAgain = await paymentAgain.ContinueToReviewAsync();

        // Then the checkout shows the review step and the message "Your payment is already authorized."
        Assert.True(reviewAgain.IsOnPage, "the checkout should show the review step");
        Assert.Equal("Your payment is already authorized.", await reviewAgain.NoticeMessageAsync());

        // And the payment provider received one payment request for that checkout session — the session's id is shown
        // once the order is placed, as its order reference
        var checkoutReference = await (await reviewAgain.PlaceOrderAsync()).OrderReferenceAsync();
        Assert.Single(PaymentProviderStub.PaymentRequestsNaming(checkoutReference));
    }
}