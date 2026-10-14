using System.Text.Json;

using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitDelivery;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Application.Session.StartCheckout;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Product.Application.GetAllProducts;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DcaShop.IntegrationTests;

/// <summary>
/// Every payment request names the checkout it pays for: the payment use case through its input port, the shop wired
/// to a payment provider that is a stub on a free port, answering per checkout reference what the test arranges.
/// </summary>
public sealed class PaymentReferenceTest : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string ProviderId = "provider";

    private readonly WireMockServer _provider = WireMockServer.Start();
    private readonly WebApplicationFactory<Program> _shop;

    public PaymentReferenceTest(WebApplicationFactory<Program> factory) =>
        _shop = factory.WithWebHostBuilder(builder => builder
            .UseSetting("Checkout:PaymentProvider:BaseUrl", _provider.Url)
            .UseSetting("Checkout:PaymentProvider:Timeout", "00:00:30"));

    [Fact]
    public async Task TwoCheckoutsSendTwoDifferentReferences()
    {
        ProviderAuthorizesEveryPayment();
        var first = await CheckoutAtThePaymentStepAsync();
        var second = await CheckoutAtThePaymentStepAsync();

        await PayAsync(first);
        await PayAsync(second);

        var references = MerchantReferences();
        Assert.Equal(2, references.Count);
        Assert.All(references, reference => Assert.False(string.IsNullOrEmpty(reference)));
        Assert.NotEqual(references[0], references[1]);
    }

    [Fact]
    public async Task TheProviderRefusingOneCheckoutLeavesTheOtherCheckoutPaid()
    {
        var first = await CheckoutAtThePaymentStepAsync();
        var second = await CheckoutAtThePaymentStepAsync();
        ProviderAuthorizesEveryPayment();
        ProviderRefusesThePaymentOf(first.SessionId);

        var refused = await Record.ExceptionAsync(() => PayAsync(first));
        var paid = await PayAsync(second);

        Assert.IsType<PaymentInitiationFailedException>(refused);
        Assert.Equal(CheckoutStep.Payment, await CurrentStepAsync(first));
        Assert.Equal(CheckoutStep.Review, paid.CurrentStep);
    }

    [Fact]
    public async Task ASecondAttemptAfterARefusalNamesTheSameCheckout()
    {
        var checkout = await CheckoutAtThePaymentStepAsync();
        _provider.Given(Request.Create().WithPath("/payments").UsingPost()).RespondWith(Response.Create().WithStatusCode(402));
        await Assert.ThrowsAsync<PaymentInitiationFailedException>(() => PayAsync(checkout));
        _provider.ResetMappings();
        ProviderAuthorizesEveryPayment();

        await PayAsync(checkout);

        var references = MerchantReferences();
        Assert.Equal(2, references.Count(reference => reference == checkout.SessionId.ToString()));
        Assert.Equal(checkout.SessionId.ToString(), Assert.Single(references.Distinct()));
    }

    private void ProviderAuthorizesEveryPayment() =>
        _provider.Given(Request.Create().WithPath("/payments").UsingPost()).AtPriority(10)
            .RespondWith(Response.Create().WithStatusCode(201).WithBodyAsJson(new { reference = "pay-" + Guid.NewGuid() }));

    private void ProviderRefusesThePaymentOf(Guid sessionId) =>
        _provider.Given(Request.Create().WithPath("/payments").UsingPost().WithBody(new JsonPartialMatcher(new { merchantReference = sessionId.ToString() })))
            .AtPriority(1)
            .RespondWith(Response.Create().WithStatusCode(402));

    /// <summary>The <c>merchantReference</c> of every payment request the provider received, in order; null where a request carried none.</summary>
    private List<string?> MerchantReferences() =>
        _provider.LogEntries
            .Select(entry => entry.RequestMessage!)
            .Where(request => request.Method == "POST" && request.Path == "/payments")
            .Select(request =>
            {
                using var body = JsonDocument.Parse(request.Body!);
                return body.RootElement.TryGetProperty("merchantReference", out var reference) ? reference.GetString() : null;
            })
            .ToList();

    private async Task<SubmitPaymentResult> PayAsync(CustomersCheckout checkout)
    {
        using var scope = _shop.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISubmitPaymentInputPort>()
            .ExecuteAsync(new SubmitPaymentCommand(checkout.SessionId, checkout.CustomerId, ProviderId));
    }

    private async Task<CheckoutStep> CurrentStepAsync(CustomersCheckout checkout)
    {
        using var scope = _shop.Services.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<IGetCheckoutSessionInputPort>()
            .ExecuteAsync(new GetCheckoutSessionQuery(checkout.SessionId, checkout.CustomerId));
        return found.Session!.Step;
    }

    /// <summary>A customer of their own whose checkout of one item has passed the buyer and the delivery step.</summary>
    private async Task<CustomersCheckout> CheckoutAtThePaymentStepAsync()
    {
        var customerId = $"payer-{Guid.NewGuid()}";
        using var scope = _shop.Services.CreateScope();
        var services = scope.ServiceProvider;

        var cart = await services.GetRequiredService<IGetOrCreateActiveCartInputPort>().ExecuteAsync(new GetOrCreateActiveCartCommand(customerId));
        var product = (await services.GetRequiredService<IGetAllProductsInputPort>().ExecuteAsync(new GetAllProductsQuery())).Products[0];
        await services.GetRequiredService<IAddItemToCartInputPort>().ExecuteAsync(new AddItemToCartCommand(cart.CartId, customerId, product.ProductId.Value, 1));
        var sessionId = (await services.GetRequiredService<IStartCheckoutInputPort>().ExecuteAsync(new StartCheckoutCommand(cart.CartId, customerId))).SessionId;

        await services.GetRequiredService<ISubmitBuyerInfoInputPort>().ExecuteAsync(
            new SubmitBuyerInfoCommand(sessionId, customerId, "ada@example.com", "Ada", "Lovelace", "+1-555-0100"));
        await services.GetRequiredService<ISubmitDeliveryInputPort>().ExecuteAsync(
            new SubmitDeliveryCommand(sessionId, customerId, "Analytical Engine Way 1", null, "London", "12345", "UK", null, "standard"));

        return new CustomersCheckout(sessionId, customerId);
    }

    private sealed record CustomersCheckout(Guid SessionId, string CustomerId);

    public void Dispose()
    {
        _shop.Dispose();
        _provider.Stop();
    }
}