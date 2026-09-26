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

    [E2eFact(DisplayName = "A payment the provider authorizes moves the checkout on to the review step")]
    public async Task APaymentTheProviderAuthorizesLeadsOnToTheReview()
    {
        // Given a checkout session at the payment step
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var detail = await catalog.ViewFirstProductAsync();
        await detail.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        var delivery = await (await buyer.FillBuyerInfoAsync("payer@example.com", "Test", "Payer", "+1-555-0100")).ContinueToDeliveryAsync();
        await delivery.FillAddressAsync("123 Main Street", "Springfield", "12345", "United States", "IL");
        await delivery.SelectFirstShippingOptionAsync();
        var payment = await delivery.ContinueToPaymentAsync();
        var sessionTotal = PaymentProviderStub.ParseShownAmount(await payment.TotalAsync());

        // And the payment provider authorizes payments
        PaymentProviderStub.AuthorizesPayments();

        // When the customer pays through the payment provider
        await payment.SelectFirstPaymentProviderAsync();
        var review = await payment.ContinueToReviewAsync();

        // Then the checkout shows the review step
        Assert.True(review.IsOnPage, "the checkout should show the review step");

        // And the payment provider received one payment request for the checkout session's total
        var request = Assert.Single(PaymentProviderStub.PaymentRequests());
        Assert.Equal(sessionTotal, request);
    }
}