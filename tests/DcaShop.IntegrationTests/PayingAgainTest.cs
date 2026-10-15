using System.Text.Json;

using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitDelivery;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Application.Session.StartCheckout;
using DcaShop.Checkout.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Paying again for a checkout whose payment the provider authorized: the payment use case through its input port, the
/// shop wired to a payment provider that is a stub on a free port.
/// </summary>
public sealed class PayingAgainTest : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string ProviderId = "provider";

    private readonly WireMockServer _provider = WireMockServer.Start();
    private readonly WebApplicationFactory<Program> _shop;

    public PayingAgainTest(WebApplicationFactory<Program> factory) =>
        _shop = factory.WithWebHostBuilder(builder => builder
            .UseSetting("Checkout:PaymentProvider:BaseUrl", _provider.Url)
            .UseSetting("Checkout:PaymentProvider:Timeout", "00:00:30"));

    [Fact]
    public async Task PayingAgainForAChangedTotalCancelsTheFirstPaymentAndRequestsTheNewTotal()
    {
        // Given a checkout session with "Domain-Driven Design" once and "Standard Shipping", whose payment of "59.98"
        // the payment provider authorized
        ProviderAuthorizesPaymentsAndCancellations();
        var checkout = await CheckoutAtThePaymentStepAsync("standard");
        await PayAsync(checkout);
        var firstPayment = await PaymentReferenceAsync(checkout);

        // And the customer went back from the review to the delivery step and chose "Express Shipping"
        await SubmitDeliveryAsync(checkout, "express");

        // When the customer pays through the payment provider again
        var paid = await PayAsync(checkout);

        // Then the payment provider received the cancellation of the first payment
        var requests = ProviderRequests();
        var cancellation = requests.FindIndex(request => request.Path == $"/payments/{firstPayment}/cancel");
        Assert.True(cancellation >= 0, $"the provider should have received the cancellation of {firstPayment}");

        // And a second payment request of "64.98" with the same merchantReference as the first
        var payments = requests.Where(request => request.Path == "/payments").ToList();
        Assert.Equal(new decimal?[] { 59.98m, 64.98m }, payments.Select(request => request.Amount));
        Assert.Equal(payments[0].MerchantReference, payments[1].MerchantReference);
        Assert.True(cancellation < requests.IndexOf(payments[1]), "the first payment should be cancelled before the new one is requested");

        // And the checkout shows the review step
        Assert.Equal(CheckoutStep.Review, paid.CurrentStep);
    }

    private void ProviderAuthorizesPaymentsAndCancellations()
    {
        _provider.Given(Request.Create().WithPath("/payments").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(201).WithBodyAsJson(new { reference = "pay-" + Guid.NewGuid() }));
        _provider.Given(Request.Create().WithPath("/payments/*/cancel").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(204));
    }

    /// <summary>Every request the provider received, in order: its path and, for a payment request, amount and checkout reference.</summary>
    private List<ProviderRequest> ProviderRequests() =>
        _provider.LogEntries
            .Select(entry => entry.RequestMessage!)
            .Where(request => request.Method == "POST")
            .Select(request =>
            {
                if (request.Path != "/payments")
                {
                    return new ProviderRequest(request.Path, null, null);
                }

                using var body = JsonDocument.Parse(request.Body!);
                return new ProviderRequest(
                    request.Path,
                    decimal.Parse(body.RootElement.GetProperty("amount").GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                    body.RootElement.GetProperty("merchantReference").GetString());
            })
            .ToList();

    private async Task<SubmitPaymentResult> PayAsync(CustomersCheckout checkout)
    {
        using var scope = _shop.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISubmitPaymentInputPort>()
            .ExecuteAsync(new SubmitPaymentCommand(checkout.SessionId, checkout.CustomerId, ProviderId));
    }

    private async Task<string> PaymentReferenceAsync(CustomersCheckout checkout)
    {
        using var scope = _shop.Services.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<IGetCheckoutSessionInputPort>()
            .ExecuteAsync(new GetCheckoutSessionQuery(checkout.SessionId, checkout.CustomerId));
        return found.Session!.PaymentSelection!.ProviderReference!;
    }

    private async Task SubmitDeliveryAsync(CustomersCheckout checkout, string shippingOption)
    {
        using var scope = _shop.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISubmitDeliveryInputPort>().ExecuteAsync(
            new SubmitDeliveryCommand(checkout.SessionId, checkout.CustomerId, "Analytical Engine Way 1", null, "London", "12345", "UK", null, shippingOption));
    }

    /// <summary>A customer of their own whose checkout of "Domain-Driven Design" once has passed the buyer and the delivery step.</summary>
    private async Task<CustomersCheckout> CheckoutAtThePaymentStepAsync(string shippingOption)
    {
        var customerId = $"payer-{Guid.NewGuid()}";
        var product = Guid.Parse(await _shop.ProductIdAsync("Domain-Driven Design"));
        Guid sessionId;
        using (var scope = _shop.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var cart = await services.GetRequiredService<IGetOrCreateActiveCartInputPort>().ExecuteAsync(new GetOrCreateActiveCartCommand(customerId));
            await services.GetRequiredService<IAddItemToCartInputPort>().ExecuteAsync(new AddItemToCartCommand(cart.CartId, customerId, product, 1));
            sessionId = (await services.GetRequiredService<IStartCheckoutInputPort>().ExecuteAsync(new StartCheckoutCommand(cart.CartId, customerId))).SessionId;
            await services.GetRequiredService<ISubmitBuyerInfoInputPort>().ExecuteAsync(
                new SubmitBuyerInfoCommand(sessionId, customerId, "ada@example.com", "Ada", "Lovelace", "+1-555-0100"));
        }

        var checkout = new CustomersCheckout(sessionId, customerId);
        await SubmitDeliveryAsync(checkout, shippingOption);
        return checkout;
    }

    private sealed record CustomersCheckout(Guid SessionId, string CustomerId);

    private sealed record ProviderRequest(string Path, decimal? Amount, string? MerchantReference);

    public void Dispose()
    {
        _shop.Dispose();
        _provider.Stop();
    }
}