using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// Tests that arrange the suite's payment provider stub. They run alone, after the parallel suites, so the requests
/// the stub records are this test's own.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PaymentProviderCollection
{
    public const string Name = "payment provider";
}

/// <summary>Paying at checkout through the payment provider, in the browser.</summary>
[Collection(PaymentProviderCollection.Name)]
public sealed class ProviderPaymentE2eTest : BaseE2eTest
{
    public ProviderPaymentE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "Place Order has the payment provider authorize the total the review showed and opens the confirmation")]
    public async Task PlaceOrderHasTheProviderAuthorizeTheReviewedTotalAndOpensTheConfirmation()
    {
        // Given a guest at the review with "Domain-Driven Design" once and "Standard Shipping", Total "59.98 EUR"
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var detail = await catalog.ViewProductAsync("Domain-Driven Design");
        await detail.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        var delivery = await (await buyer.FillBuyerInfoAsync("jane@example.com", "Jane", "Doe", "+49 30 1234567")).ContinueToDeliveryAsync();
        await delivery.FillAddressAsync("Hauptstrasse 1", "Berlin", "10115", "Germany", string.Empty);
        await delivery.ChooseShippingMethodAsync("Standard Shipping");
        var payment = await delivery.ContinueToPaymentAsync();
        await payment.ChooseTheOfferedPaymentProviderAsync();
        var review = await payment.ContinueToReviewAsync();
        Assert.Equal("59.98 EUR", await review.TotalAsync());

        // And the payment provider authorizes payments — from here on, the requests it records are this order's
        PaymentProviderStub.AuthorizesPayments();

        // When the guest presses "Place Order"
        var confirmation = await review.PlaceOrderAsync();

        // Then the confirmation page opens at /checkout/confirmation with "Order Total" "59.98 EUR"
        Assert.Equal(("Order Total", "59.98 EUR"), await confirmation.OrderTotalAsync());

        // And the payment provider received one payment request, of "59.98"
        var request = Assert.Single(PaymentProviderStub.PaymentRequests());
        Assert.Equal((59.98m, "EUR"), request);
    }
}