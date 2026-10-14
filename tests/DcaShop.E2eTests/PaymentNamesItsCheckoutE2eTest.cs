using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// A payment request names the checkout it pays for. The request is told from the other tests' payments by that very
/// reference, so the test shares the suite's payment provider stub and runs beside the others.
/// </summary>
public sealed class PaymentNamesItsCheckoutE2eTest : BaseE2eTest
{
    public PaymentNamesItsCheckoutE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "The payment request names its checkout")]
    public async Task ThePaymentRequestCarriesTheCheckoutSessionAsItsMerchantReference()
    {
        // Given a checkout session at the payment step
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var detail = await catalog.ViewProductAsync("Domain-Driven Design");
        await detail.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        var delivery = await (await buyer.FillBuyerInfoAsync("payer@example.com", "Test", "Payer", "+1-555-0100")).ContinueToDeliveryAsync();
        await delivery.FillAddressAsync("123 Main Street", "Springfield", "12345", "United States", "IL");
        await delivery.ChooseShippingMethodAsync("Standard Shipping");
        var payment = await delivery.ContinueToPaymentAsync();
        var sessionTotal = PaymentProviderStub.ParseShownAmount(await payment.TotalAsync());

        // And the payment provider authorizes payments — the suite's stub does unless a test arranges otherwise

        // When the customer pays through the payment provider
        await payment.ChooseTheOfferedPaymentProviderAsync();
        var review = await payment.ContinueToReviewAsync();

        // The checkout session's id is shown once the order is placed, as its order reference
        var checkoutReference = await (await review.PlaceOrderAsync()).OrderReferenceAsync();

        // Then the payment provider received one payment request for the checkout session's total
        // And that request carries the checkout reference, the checkout session's id as text, as merchantReference
        var request = Assert.Single(PaymentProviderStub.PaymentRequestsNaming(checkoutReference));
        Assert.Equal(sessionTotal, request);
    }
}