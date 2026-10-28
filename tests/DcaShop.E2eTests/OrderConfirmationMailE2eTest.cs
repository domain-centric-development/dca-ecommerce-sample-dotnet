using DcaShop.E2eTests.Pages;

namespace DcaShop.E2eTests;

/// <summary>
/// Tests that read the mails the suite's mail provider stub recorded. They run alone, after the parallel suites, so the
/// mails the stub records are this test's own.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MailProviderCollection
{
    public const string Name = "mail provider";
}

/// <summary>The buyer's order confirmation mail, after the order is placed in the browser.</summary>
[Collection(MailProviderCollection.Name)]
public sealed class OrderConfirmationMailE2eTest : BaseE2eTest
{
    private const string Jane = "jane@example.com";

    public OrderConfirmationMailE2eTest(BrowserFixture browser) : base(browser)
    {
    }

    [E2eFact(DisplayName = "The buyer gets the order confirmation")]
    public async Task PlacingTheOrderSendsTheOrderConfirmationWithTheShownTotalToTheBuyer()
    {
        // Given a guest at the review whose buyer e-mail is "jane@example.com"
        var catalog = await ProductCatalogPage.NavigateToAsync(Page);
        var detail = await catalog.ViewProductAsync("Domain-Driven Design");
        await detail.AddToCartAsync();
        var cart = await CartPage.NavigateToAsync(Page);
        var buyer = await cart.ProceedToCheckoutAsync();
        var delivery = await (await buyer.FillBuyerInfoAsync(Jane, "Jane", "Schmidt", "+49 40 7654321")).ContinueToDeliveryAsync();
        await delivery.FillAddressAsync("Hauptstrasse 1", "Berlin", "10115", "Germany", string.Empty);
        await delivery.ChooseShippingMethodAsync("Standard Shipping");
        var payment = await delivery.ContinueToPaymentAsync();
        await payment.ChooseTheOfferedPaymentProviderAsync();
        var review = await payment.ContinueToReviewAsync();
        MailProviderStub.AcceptsMails();

        // When the guest presses "Place Order"
        var confirmation = await review.PlaceOrderAsync();
        var (_, shownTotal) = await confirmation.OrderTotalAsync();

        // Then shortly afterwards the mail provider has received one mail with the template "order-confirmation" to "jane@example.com"
        var mail = Assert.Single(await MailProviderStub.MailsWithinFifteenSecondsToAsync(Jane));
        Assert.Equal("order-confirmation", mail.Template);

        // And the mail carries the order total the confirmation page shows
        Assert.Equal(shownTotal, $"{mail.OrderTotal} {mail.Currency}");
    }
}