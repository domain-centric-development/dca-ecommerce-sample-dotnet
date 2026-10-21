using System.Diagnostics;

using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;

using WireMock.Server;

namespace DcaShop.IntegrationTests;

/// <summary>
/// When the payment provider is asked: the payment step only chooses how to pay, and "Place Order" asks for the payment
/// once the order's checks and its stock hold. Through the input ports, the shop wired to a payment provider that is a
/// stub on a free port; each test a shop of its own, so every product starts at its seeded stock.
/// </summary>
public sealed class PlaceOrderPaymentTest : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string Hexagon = "Wooden Hexagon Desk Model";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly WireMockServer _provider = WireMockServer.Start();
    private WebApplicationFactory<Program>? _shop;

    public PlaceOrderPaymentTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ChoosingHowToPayOpensTheReviewWithoutAPaymentRequest()
    {
        var checkouts = Checkouts();

        // Given a checkout session at the payment step
        var checkout = await checkouts.AtThePaymentStepAsync("standard", ("Domain-Driven Design", 1));

        // And the payment provider authorizes payments
        _provider.AuthorizesPayments();

        // When the customer chooses the provider and presses "Continue to Review"
        var chosen = await checkouts.ChooseTheProviderAsync(checkout);

        // Then the checkout shows the review step
        Assert.Equal(CheckoutStep.Review, chosen.CurrentStep);

        // And the payment provider received no payment request
        Assert.Empty(_provider.PaymentRequests());
    }

    [Fact]
    public async Task ARefusedPaymentAtPlaceOrderHoldsNoStock()
    {
        var checkouts = Checkouts();

        // Given a guest at the review with "Wooden Hexagon Desk Model" twice, whose product page reads "8 units available"
        _provider.AuthorizesPayments();
        var checkout = await checkouts.AtTheReviewAsync("standard", (Hexagon, 2));
        Assert.Equal(8, await checkouts.StockAsync(Hexagon));

        // And the payment provider refuses payments
        _provider.RefusesPayments();

        // When the guest presses "Place Order"
        var refused = await Record.ExceptionAsync(() => checkouts.PlaceOrderAsync(checkout));

        // Then the review stays open with the refusal
        Assert.IsType<PaymentInitiationFailedException>(refused);
        var session = await checkouts.SessionAsync(checkout);
        Assert.Equal((CheckoutStep.Review, CheckoutSessionStatus.Active), (session.Step, session.Status));

        // And the stock of "Wooden Hexagon Desk Model" is still 8
        Assert.Equal(8, await checkouts.StockAsync(Hexagon));
    }

    [Fact]
    public async Task AProviderThatAnswersPlaceOrderOnlyAfterFiveSecondsCountsAsUnavailable()
    {
        var checkouts = Checkouts(providerTimeout: "00:00:02");

        // Given a checkout session at the review
        _provider.AuthorizesPayments();
        var checkout = await checkouts.AtTheReviewAsync("standard", ("Domain-Driven Design", 1));

        // And the payment provider answers only after 5 seconds
        _provider.AuthorizesOnlyAfter(TimeSpan.FromSeconds(5));

        // When the customer presses "Place Order"
        var watch = Stopwatch.StartNew();
        var failure = await Record.ExceptionAsync(() => checkouts.PlaceOrderAsync(checkout));
        watch.Stop();

        // Then the order is refused as the provider being unavailable, without waiting for the late answer
        Assert.IsType<PaymentProviderUnavailableException>(failure);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(4), $"the late answer was awaited ({watch.Elapsed.TotalSeconds:0.0} s) instead of being abandoned at the timeout");

        // And no order is confirmed: the checkout stays at the review
        var session = await checkouts.SessionAsync(checkout);
        Assert.Equal((CheckoutStep.Review, CheckoutSessionStatus.Active), (session.Step, session.Status));
    }

    [Fact]
    public async Task APlaceOrderRefusedForTheStockRequestsNoPayment()
    {
        var checkouts = Checkouts();
        _provider.AuthorizesPayments();

        // Given a guest at the review with "Wooden Hexagon Desk Model" five times
        var checkout = await checkouts.AtTheReviewAsync("standard", (Hexagon, 5));

        // And another visitor has since bought it five times, leaving 3
        await GuestOrders.AnotherGuestHasBoughtAsync(_shop!, Guid.Parse(await _shop!.ProductIdAsync(Hexagon)), 5);
        Assert.Equal(3, await checkouts.StockAsync(Hexagon));

        // When the guest presses "Place Order"
        var refused = await Record.ExceptionAsync(() => checkouts.PlaceOrderAsync(checkout));

        // Then the order is refused, naming the line the stock no longer covers
        var invalid = Assert.IsType<CheckoutValidationException>(refused);
        Assert.Equal(Hexagon, invalid.Validation.ShortOfStock?.ProductName);

        // And the payment provider received no payment request
        Assert.Empty(_provider.PaymentRequestsFor(checkout));
    }

    /// <summary>This test's own shop, wired to the provider stub and awaiting it at most <paramref name="providerTimeout"/>.</summary>
    private PortCheckouts Checkouts(string providerTimeout = "00:00:30")
    {
        _shop = PortCheckouts.ShopWith(_factory, _provider, providerTimeout);
        return new PortCheckouts(_shop);
    }

    public void Dispose()
    {
        _shop?.Dispose();
        _provider.Stop();
    }
}