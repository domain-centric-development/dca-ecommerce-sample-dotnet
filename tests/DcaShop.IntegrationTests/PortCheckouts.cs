using System.Globalization;
using System.Text.Json;

using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitDelivery;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Application.Session.StartCheckout;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Inventory.Application.GetStockForProducts;
using DcaShop.SharedKernel.Domain.Model;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DcaShop.IntegrationTests;

/// <summary>A customer's checkout session, as the checkout's input ports name it.</summary>
internal sealed record CustomersCheckout(Guid SessionId, string CustomerId);

/// <summary>
/// Checkouts driven through the checkout's input ports, in a shop wired to a payment provider that is a stub on a free
/// port: each customer of their own, with the products and the shipping method the test names.
/// </summary>
internal sealed class PortCheckouts(WebApplicationFactory<Program> shop)
{
    /// <summary>The way to pay the shop offers when it is wired to the provider.</summary>
    public const string Provider = "provider";

    /// <summary>The shop wired to <paramref name="provider"/>, awaiting it at most <paramref name="timeout"/>.</summary>
    public static WebApplicationFactory<Program> ShopWith(WebApplicationFactory<Program> factory, WireMockServer provider, string timeout = "00:00:30") =>
        factory.WithWebHostBuilder(builder => builder
            .UseSetting("Checkout:PaymentProvider:BaseUrl", provider.Url)
            .UseSetting("Checkout:PaymentProvider:Timeout", timeout));

    /// <summary>A customer of their own whose checkout of the products has passed the buyer and the delivery step.</summary>
    public async Task<CustomersCheckout> AtThePaymentStepAsync(string shippingOption, params (string Product, int Quantity)[] lines)
    {
        var customerId = $"payer-{Guid.NewGuid()}";
        var products = new List<(Guid Id, int Quantity)>();
        foreach (var (product, quantity) in lines)
        {
            products.Add((Guid.Parse(await shop.ProductIdAsync(product)), quantity));
        }

        using var scope = shop.Services.CreateScope();
        var services = scope.ServiceProvider;
        var cart = await services.GetRequiredService<IGetOrCreateActiveCartInputPort>().ExecuteAsync(new GetOrCreateActiveCartCommand(customerId));
        foreach (var (id, quantity) in products)
        {
            await services.GetRequiredService<IAddItemToCartInputPort>().ExecuteAsync(new AddItemToCartCommand(cart.CartId, customerId, id, quantity));
        }

        var sessionId = (await services.GetRequiredService<IStartCheckoutInputPort>().ExecuteAsync(new StartCheckoutCommand(cart.CartId, customerId))).SessionId;
        await services.GetRequiredService<ISubmitBuyerInfoInputPort>().ExecuteAsync(
            new SubmitBuyerInfoCommand(sessionId, customerId, "jane@example.com", "Jane", "Doe", "+49 30 1234567"));
        await services.GetRequiredService<ISubmitDeliveryInputPort>().ExecuteAsync(
            new SubmitDeliveryCommand(sessionId, customerId, "Hauptstrasse 1", null, "Berlin", "10115", "Germany", null, shippingOption));
        return new CustomersCheckout(sessionId, customerId);
    }

    /// <summary>A customer of their own at the review: the products, the shipping method and the provider chosen.</summary>
    public async Task<CustomersCheckout> AtTheReviewAsync(string shippingOption, params (string Product, int Quantity)[] lines)
    {
        var checkout = await AtThePaymentStepAsync(shippingOption, lines);
        await ChooseTheProviderAsync(checkout);
        return checkout;
    }

    /// <summary>The customer chooses the provider on the payment step and continues to the review.</summary>
    public async Task<SubmitPaymentResult> ChooseTheProviderAsync(CustomersCheckout checkout)
    {
        using var scope = shop.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISubmitPaymentInputPort>()
            .ExecuteAsync(new SubmitPaymentCommand(checkout.SessionId, checkout.CustomerId, Provider));
    }

    /// <summary>The customer presses "Place Order".</summary>
    public async Task<ConfirmCheckoutResult> PlaceOrderAsync(CustomersCheckout checkout)
    {
        using var scope = shop.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IConfirmCheckoutInputPort>()
            .ExecuteAsync(new ConfirmCheckoutCommand(checkout.SessionId, checkout.CustomerId));
    }

    /// <summary>The checkout session as it is stored now.</summary>
    public async Task<CheckoutCartSnapshot> SessionAsync(CustomersCheckout checkout)
    {
        using var scope = shop.Services.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<IGetCheckoutSessionInputPort>()
            .ExecuteAsync(new GetCheckoutSessionQuery(checkout.SessionId, checkout.CustomerId));
        return found.Session!;
    }

    /// <summary>The stock of the product as Inventory holds it now.</summary>
    public async Task<int> StockAsync(string product)
    {
        var productId = new ProductId(Guid.Parse(await shop.ProductIdAsync(product)));
        using var scope = shop.Services.CreateScope();
        var stocks = await scope.ServiceProvider.GetRequiredService<IGetStockForProductsInputPort>()
            .ExecuteAsync(new GetStockForProductsQuery([productId]));
        return stocks.Stocks[productId].AvailableStock;
    }
}

/// <summary>What the payment provider stub is told to answer, and the payment requests it received.</summary>
internal static class PaymentProviderStubs
{
    /// <summary>Every payment request is authorized, each under a payment reference of its own.</summary>
    public static void AuthorizesPayments(this WireMockServer provider) =>
        provider.Given(Request.Create().WithPath("/payments").UsingPost()).AtPriority(10)
            .RespondWith(Response.Create().WithStatusCode(201).WithBodyAsJson(new { reference = "pay-" + Guid.NewGuid() }));

    /// <summary>Every payment request is refused.</summary>
    public static void RefusesPayments(this WireMockServer provider) =>
        provider.Given(Request.Create().WithPath("/payments").UsingPost()).AtPriority(5)
            .RespondWith(Response.Create().WithStatusCode(402));

    /// <summary>The payment requests naming the checkout reference of <paramref name="checkout"/> are refused.</summary>
    public static void RefusesThePaymentOf(this WireMockServer provider, CustomersCheckout checkout) =>
        provider.Given(Request.Create().WithPath("/payments").UsingPost().WithBody(new JsonPartialMatcher(new { merchantReference = checkout.SessionId.ToString() })))
            .AtPriority(1)
            .RespondWith(Response.Create().WithStatusCode(402));

    /// <summary>Every payment request is authorized, but only after <paramref name="delay"/>.</summary>
    public static void AuthorizesOnlyAfter(this WireMockServer provider, TimeSpan delay) =>
        provider.Given(Request.Create().WithPath("/payments").UsingPost()).AtPriority(1)
            .RespondWith(Response.Create().WithStatusCode(201).WithBodyAsJson(new { reference = "late" }).WithDelay(delay));

    /// <summary>Every payment request the provider received, in order: its amount and its checkout reference.</summary>
    public static List<(decimal Amount, string? MerchantReference)> PaymentRequests(this WireMockServer provider) =>
        provider.LogEntries
            .Select(entry => entry.RequestMessage!)
            .Where(request => request.Method == "POST" && request.Path == "/payments")
            .Select(request =>
            {
                using var body = JsonDocument.Parse(request.Body!);
                var amount = decimal.Parse(body.RootElement.GetProperty("amount").GetString()!, CultureInfo.InvariantCulture);
                var reference = body.RootElement.TryGetProperty("merchantReference", out var named) ? named.GetString() : null;
                return (amount, reference);
            })
            .ToList();

    /// <summary>The payment requests the provider received for <paramref name="checkout"/>.</summary>
    public static List<(decimal Amount, string? MerchantReference)> PaymentRequestsFor(this WireMockServer provider, CustomersCheckout checkout) =>
        provider.PaymentRequests().Where(request => request.MerchantReference == checkout.SessionId.ToString()).ToList();
}