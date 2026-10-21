using System.Net;

using DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The page's translation of a payment "Place Order" could not get: the review again, at its own address, with the
/// shop's own sentence and no confirmation — against a stubbed "Place Order" that answers with the provider's refusal
/// or its absence.
/// </summary>
public sealed class PlaceOrderPaymentRefusalPageTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PlaceOrderPaymentRefusalPageTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ARefusedPaymentKeepsTheCustomerAtTheReviewWithTheRefusalText()
    {
        // Given a checkout session at the review
        // And the payment provider refuses payments
        var guest = await GuestAtTheReviewOfAShopWhosePlaceOrderAnswers(
            new PaymentInitiationFailedException(PaymentProviderId.Of("provider"), "The payment provider refused the payment"));

        // When the customer presses "Place Order"
        var placed = await PlaceOrderAsync(guest);

        // Then the review stays open at /checkout/review with the refusal
        Assert.Equal(HttpStatusCode.Redirect, placed.StatusCode);
        Assert.Equal("/checkout/review", placed.Headers.Location!.ToString());
        var review = await guest.GetStringAsync(placed.Headers.Location);
        Assert.Equal(
            "The payment was refused. Please choose another way to pay.",
            AvailabilityPages.Text(review, @"data-test=""review-error-message"">([^<]*)</div>"));

        // And no confirmation is shown
        Assert.DoesNotContain("data-test=\"confirmation-title\"", review, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnUnavailableProviderKeepsTheCustomerAtTheReviewWithTheUnavailableText()
    {
        // Given a checkout session at the review
        // And the payment provider answers only after 5 seconds — which "Place Order" answers as unavailable
        var guest = await GuestAtTheReviewOfAShopWhosePlaceOrderAnswers(
            new PaymentProviderUnavailableException(PaymentProviderId.Of("provider")));

        // When the customer presses "Place Order"
        var placed = await PlaceOrderAsync(guest);

        // Then the review stays open at /checkout/review with the unavailable text
        Assert.Equal(HttpStatusCode.Redirect, placed.StatusCode);
        Assert.Equal("/checkout/review", placed.Headers.Location!.ToString());
        var review = await guest.GetStringAsync(placed.Headers.Location);
        Assert.Equal(
            "The payment provider is not available right now. Please try again later.",
            AvailabilityPages.Text(review, @"data-test=""review-error-message"">([^<]*)</div>"));

        // And no confirmation is shown
        Assert.DoesNotContain("data-test=\"confirmation-title\"", review, StringComparison.Ordinal);
    }

    /// <summary>A guest at the review of "Domain-Driven Design" once, in a shop whose "Place Order" fails with <paramref name="refusal"/>.</summary>
    private async Task<HttpClient> GuestAtTheReviewOfAShopWhosePlaceOrderAnswers(Exception refusal)
    {
        var shop = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<IConfirmCheckoutInputPort>(new Refusing(refusal))));
        var guest = GuestOrders.Guest(shop);
        await GuestOrders.GoToTheReviewAsync(guest, (await AvailabilityPages.ProductPathAsync(guest, "Domain-Driven Design"), 1));
        return guest;
    }

    private static async Task<HttpResponseMessage> PlaceOrderAsync(HttpClient guest) =>
        await guest.PostAsync("/checkout/confirm", GuestOrders.Form(await guest.GetStringAsync("/checkout/review"), ("placedFrom", "review")));

    /// <summary>A "Place Order" that refuses every order with the same failure.</summary>
    private sealed class Refusing(Exception refusal) : IConfirmCheckoutInputPort
    {
        public Task<ConfirmCheckoutResult> ExecuteAsync(ConfirmCheckoutCommand command, CancellationToken cancellationToken = default) =>
            Task.FromException<ConfirmCheckoutResult>(refusal);
    }
}