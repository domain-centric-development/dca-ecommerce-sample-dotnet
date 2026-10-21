using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;

using WireMock.Server;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Every payment request "Place Order" sends names the checkout it pays for: the order through its input port, the shop
/// wired to a payment provider that is a stub on a free port, answering per checkout reference what the test arranges.
/// </summary>
public sealed class PaymentReferenceTest : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WireMockServer _provider = WireMockServer.Start();
    private readonly WebApplicationFactory<Program> _shop;
    private readonly PortCheckouts _checkouts;

    public PaymentReferenceTest(WebApplicationFactory<Program> factory)
    {
        _shop = PortCheckouts.ShopWith(factory, _provider);
        _checkouts = new PortCheckouts(_shop);
    }

    [Fact]
    public async Task PlaceOrderSendsOnePaymentRequestForTheSessionTotalNamingItsCheckout()
    {
        // Given a checkout session at the review
        _provider.AuthorizesPayments();
        var checkout = await _checkouts.AtTheReviewAsync("standard", ("Domain-Driven Design", 1));
        var total = (await _checkouts.SessionAsync(checkout)).Totals.Total;
        _provider.ResetLogEntries();

        // And the payment provider authorizes payments
        // When the customer presses "Place Order"
        await _checkouts.PlaceOrderAsync(checkout);

        // Then the payment provider received one payment request for the checkout session's total
        var request = Assert.Single(_provider.PaymentRequests());
        Assert.Equal(total.Amount, request.Amount);

        // And that request carries the checkout reference, the checkout session's id as text, as merchantReference
        Assert.Equal(checkout.SessionId.ToString(), request.MerchantReference);
    }

    [Fact]
    public async Task TwoCheckoutsSendTwoDifferentReferences()
    {
        // Given two customers, each with a checkout session at the review
        _provider.AuthorizesPayments();
        var first = await _checkouts.AtTheReviewAsync("standard", ("Domain-Driven Design", 1));
        var second = await _checkouts.AtTheReviewAsync("standard", ("Domain-Driven Design", 1));
        _provider.ResetLogEntries();

        // When both press "Place Order"
        await _checkouts.PlaceOrderAsync(first);
        await _checkouts.PlaceOrderAsync(second);

        // Then the payment provider received two payment requests with two different merchantReference values
        var references = _provider.PaymentRequests().Select(request => request.MerchantReference).ToList();
        Assert.Equal(2, references.Count);
        Assert.All(references, reference => Assert.False(string.IsNullOrEmpty(reference)));
        Assert.NotEqual(references[0], references[1]);
    }

    [Fact]
    public async Task TheProviderRefusingOneCheckoutLeavesTheOtherCheckoutConfirmed()
    {
        // Given two customers, each with a checkout session at the review
        _provider.AuthorizesPayments();
        var first = await _checkouts.AtTheReviewAsync("standard", ("Domain-Driven Design", 1));
        var second = await _checkouts.AtTheReviewAsync("standard", ("Domain-Driven Design", 1));

        // And the payment provider refuses the payment for the first customer's checkout reference only
        _provider.RefusesThePaymentOf(first);

        // When both press "Place Order"
        var refused = await Record.ExceptionAsync(() => _checkouts.PlaceOrderAsync(first));
        var placed = await _checkouts.PlaceOrderAsync(second);

        // Then the first customer stays at the review with the refusal
        Assert.IsType<PaymentInitiationFailedException>(refused);
        var firstSession = await _checkouts.SessionAsync(first);
        Assert.Equal((CheckoutStep.Review, CheckoutSessionStatus.Active), (firstSession.Step, firstSession.Status));

        // And the second customer sees the confirmation
        Assert.Equal(CheckoutSessionStatus.Confirmed, placed.Status);
    }

    [Fact]
    public async Task ASecondPlaceOrderAfterARefusalNamesTheSameCheckout()
    {
        // Given a checkout session at the review
        _provider.AuthorizesPayments();
        var checkout = await _checkouts.AtTheReviewAsync("standard", ("Domain-Driven Design", 1));
        _provider.ResetLogEntries();

        // And the customer's first "Place Order" was refused by the payment provider
        _provider.RefusesPayments();
        await Assert.ThrowsAsync<PaymentInitiationFailedException>(() => _checkouts.PlaceOrderAsync(checkout));
        _provider.ResetMappings();
        _provider.AuthorizesPayments();

        // When the customer presses "Place Order" again
        await _checkouts.PlaceOrderAsync(checkout);

        // Then the payment provider received two payment requests for that checkout session
        var references = _provider.PaymentRequests().Select(request => request.MerchantReference).ToList();
        Assert.Equal(2, references.Count(reference => reference == checkout.SessionId.ToString()));

        // And both carry the same merchantReference
        Assert.Equal(checkout.SessionId.ToString(), Assert.Single(references.Distinct()));
    }

    public void Dispose()
    {
        _shop.Dispose();
        _provider.Stop();
    }
}