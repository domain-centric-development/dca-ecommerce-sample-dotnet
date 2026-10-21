using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;
using DcaShop.Checkout.Application.CheckoutCompletion.GetReview;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitDelivery;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;
using DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;
using DcaShop.Checkout.Application.Session.GetConfirmedCheckoutSession;
using DcaShop.Checkout.Application.Session.StartCheckout;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Pricing.Application.ChangePrice;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// The review shows the open checkout at today's prices, and "Place Order" orders exactly what it showed: the use cases
/// through their input ports in the wired shop. Each test starts a shop of its own, so a changed price never reaches
/// another test.
/// </summary>
public sealed class ReviewFingerprintTest
{
    private const string DomainDrivenDesign = "Domain-Driven Design";

    [Fact]
    public async Task OpeningTheReviewLaterShowsTodaysPriceAndTotalAndStoresNothing()
    {
        using var shop = new WebApplicationFactory<Program>();
        var checkout = await GuestAtTheReviewAsync(shop);
        Assert.Equal(new[] { "Domain-Driven Design x1 54.99 EUR" }, Lines((await ReviewAsync(shop, checkout)).Order));
        await PriceIsSetAsync(shop, checkout.ProductId, 49.99m);

        var reopened = await ReviewAsync(shop, checkout);

        Assert.Equal(new[] { "Domain-Driven Design x1 49.99 EUR" }, Lines(reopened.Order));
        Assert.Equal("54.98 EUR", reopened.Order.Totals.Total.ToString());
        Assert.Equal(new[] { "Domain-Driven Design x1 54.99 EUR" }, Lines(await ActiveSessionAsync(shop, checkout)));
    }

    [Fact]
    public async Task AfterAPriceRefusalTheOrderIsPlacedAtThePriceNowShown()
    {
        using var shop = new WebApplicationFactory<Program>();
        var checkout = await GuestAtTheReviewAsync(shop);
        var shown = await ReviewAsync(shop, checkout);
        await PriceIsSetAsync(shop, checkout.ProductId, 49.99m);
        await Assert.ThrowsAsync<PriceChangedSinceReviewException>(() => PlaceOrderAsync(shop, checkout, shown.Fingerprint));
        var now = await ReviewAsync(shop, checkout);
        Assert.Equal(new[] { "Domain-Driven Design x1 49.99 EUR" }, Lines(now.Order));

        await PlaceOrderAsync(shop, checkout, now.Fingerprint);

        Assert.Equal(new[] { "Domain-Driven Design x1 49.99 EUR" }, Lines(await ConfirmedSessionAsync(shop, checkout)));
    }

    /// <summary>Each line as the confirmation names it under "Items Ordered": name, quantity and line total.</summary>
    private static IReadOnlyList<string> Lines(CheckoutCartSnapshot session) =>
        session.LineItems.Select(line => $"{line.Name} x{line.Quantity} {line.LineTotal}").ToList();

    private static async Task<GetReviewResult> ReviewAsync(WebApplicationFactory<Program> shop, GuestsCheckout checkout)
    {
        using var scope = shop.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IGetReviewInputPort>()
            .ExecuteAsync(new GetReviewQuery(checkout.SessionId, checkout.CustomerId));
    }

    private static async Task PlaceOrderAsync(WebApplicationFactory<Program> shop, GuestsCheckout checkout, string fingerprint)
    {
        using var scope = shop.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IConfirmCheckoutInputPort>()
            .ExecuteAsync(new ConfirmCheckoutCommand(checkout.SessionId, checkout.CustomerId, fingerprint));
    }

    private static async Task PriceIsSetAsync(WebApplicationFactory<Program> shop, Guid productId, decimal price)
    {
        using var scope = shop.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IChangePriceInputPort>()
            .ExecuteAsync(new ChangePriceCommand(productId, price, "EUR"));
    }

    private static async Task<CheckoutCartSnapshot> ActiveSessionAsync(WebApplicationFactory<Program> shop, GuestsCheckout checkout)
    {
        using var scope = shop.Services.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<IGetActiveCheckoutSessionInputPort>()
            .ExecuteAsync(new GetActiveCheckoutSessionQuery(checkout.CustomerId));
        return found.Session!;
    }

    private static async Task<CheckoutCartSnapshot> ConfirmedSessionAsync(WebApplicationFactory<Program> shop, GuestsCheckout checkout)
    {
        using var scope = shop.Services.CreateScope();
        var found = await scope.ServiceProvider.GetRequiredService<IGetConfirmedCheckoutSessionInputPort>()
            .ExecuteAsync(new GetConfirmedCheckoutSessionQuery(checkout.CustomerId));
        return found.Session!;
    }

    /// <summary>A guest of their own with "Domain-Driven Design" once and "Standard Shipping", at the review.</summary>
    private static async Task<GuestsCheckout> GuestAtTheReviewAsync(WebApplicationFactory<Program> shop)
    {
        var customerId = $"guest-{Guid.NewGuid()}";
        var productId = Guid.Parse(await shop.ProductIdAsync(DomainDrivenDesign));
        using var scope = shop.Services.CreateScope();
        var services = scope.ServiceProvider;

        var cart = await services.GetRequiredService<IGetOrCreateActiveCartInputPort>().ExecuteAsync(new GetOrCreateActiveCartCommand(customerId));
        await services.GetRequiredService<IAddItemToCartInputPort>().ExecuteAsync(new AddItemToCartCommand(cart.CartId, customerId, productId, 1));
        var sessionId = (await services.GetRequiredService<IStartCheckoutInputPort>().ExecuteAsync(new StartCheckoutCommand(cart.CartId, customerId))).SessionId;
        await services.GetRequiredService<ISubmitBuyerInfoInputPort>().ExecuteAsync(
            new SubmitBuyerInfoCommand(sessionId, customerId, "jane@example.com", "Jane", "Doe", "+49 30 1234567"));
        await services.GetRequiredService<ISubmitDeliveryInputPort>().ExecuteAsync(
            new SubmitDeliveryCommand(sessionId, customerId, "Hauptstrasse 1", null, "Berlin", "10115", "Germany", null, "standard"));
        await services.GetRequiredService<ISubmitPaymentInputPort>().ExecuteAsync(new SubmitPaymentCommand(sessionId, customerId, "mock"));

        return new GuestsCheckout(sessionId, customerId, productId);
    }

    private sealed record GuestsCheckout(Guid SessionId, string CustomerId, Guid ProductId);
}