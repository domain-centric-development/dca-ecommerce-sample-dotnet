using DcaShop.Cart.Application.Shopping.AddItemToCart;
using DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;
using DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;
using DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;
using DcaShop.Checkout.Application.Session.GetCheckoutSession;
using DcaShop.Checkout.Application.Session.StartCheckout;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Product.Application.GetAllProducts;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DcaShop.IntegrationTests;

/// <summary>
/// A session id says which checkout, never whose. The wizard steps take the id from the outside, and the web
/// adapter resolving the visitor's <i>active</i> session is what keeps the browser flow safe — not the use cases.
/// </summary>
/// <remarks>
/// The rule therefore lives in the use case: every step asks the repository a question scoped to the caller, so a
/// session that is not theirs is indistinguishable from one that does not exist.
/// </remarks>
public sealed class CheckoutOwnershipTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CheckoutOwnershipTest(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ReadingAStrangersSessionFindsNothing()
    {
        var owner = $"owner-{Guid.NewGuid()}";
        var stranger = $"stranger-{Guid.NewGuid()}";
        var sessionId = await CheckoutOfAsync(owner);

        using var scope = _factory.Services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IGetCheckoutSessionInputPort>();

        Assert.IsType<GetCheckoutSessionResult.Found>((await query.ExecuteAsync(new GetCheckoutSessionQuery(sessionId, owner))).Value);
        Assert.IsType<GetCheckoutSessionResult.CheckoutSessionNotFound>((await query.ExecuteAsync(new GetCheckoutSessionQuery(sessionId, stranger))).Value);
    }

    [Fact]
    public async Task SubmittingBuyerInfoForAStrangersSessionIsRefused()
    {
        var owner = $"owner-{Guid.NewGuid()}";
        var stranger = $"stranger-{Guid.NewGuid()}";
        var sessionId = await CheckoutOfAsync(owner);

        using var scope = _factory.Services.CreateScope();
        var submitBuyerInfo = scope.ServiceProvider.GetRequiredService<ISubmitBuyerInfoInputPort>();

        var refused = await submitBuyerInfo.ExecuteAsync(
            new SubmitBuyerInfoCommand(sessionId, stranger, "eve@example.com", "Eve", "Adams", "+1-555-0199"));
        Assert.Equal(sessionId, Assert.IsType<SubmitBuyerInfoResult.CheckoutSessionNotFound>(refused.Value).SessionId.Value);

        // The owner is unaffected
        var accepted = await submitBuyerInfo.ExecuteAsync(
            new SubmitBuyerInfoCommand(sessionId, owner, "ada@example.com", "Ada", "Lovelace", "+1-555-0100"));
        Assert.Equal(CheckoutStep.Delivery, Assert.IsType<SubmitBuyerInfoResult.Submitted>(accepted.Value).CurrentStep);
    }

    [Fact]
    public async Task PayingForOrConfirmingAStrangersSessionIsRefused()
    {
        var owner = $"owner-{Guid.NewGuid()}";
        var stranger = $"stranger-{Guid.NewGuid()}";
        var sessionId = await CheckoutOfAsync(owner);

        using var scope = _factory.Services.CreateScope();
        var submitPayment = scope.ServiceProvider.GetRequiredService<ISubmitPaymentInputPort>();
        var confirm = scope.ServiceProvider.GetRequiredService<IConfirmCheckoutInputPort>();

        var payment = await submitPayment.ExecuteAsync(new SubmitPaymentCommand(sessionId, stranger, "mock"));
        Assert.Equal(sessionId, Assert.IsType<SubmitPaymentResult.CheckoutSessionNotFound>(payment.Value).SessionId.Value);

        var confirmation = await confirm.ExecuteAsync(new ConfirmCheckoutCommand(sessionId, stranger, string.Empty));
        Assert.Equal(sessionId, Assert.IsType<ConfirmCheckoutResult.CheckoutSessionNotFound>(confirmation.Value).SessionId.Value);
    }

    /// <summary>An active checkout session on a cart with one item, belonging to the given customer.</summary>
    private async Task<Guid> CheckoutOfAsync(string customerId)
    {
        using var scope = _factory.Services.CreateScope();
        var carts = scope.ServiceProvider.GetRequiredService<IGetOrCreateActiveCartInputPort>();
        var addItem = scope.ServiceProvider.GetRequiredService<IAddItemToCartInputPort>();
        var products = scope.ServiceProvider.GetRequiredService<IGetAllProductsInputPort>();
        var start = scope.ServiceProvider.GetRequiredService<IStartCheckoutInputPort>();

        var cart = await carts.ExecuteAsync(new GetOrCreateActiveCartCommand(customerId));
        var product = (await products.ExecuteAsync(new GetAllProductsQuery())).Products[0];
        Assert.IsType<AddItemToCartResult.Added>((await addItem.ExecuteAsync(new AddItemToCartCommand(cart.CartId, customerId, product.ProductId.Value, 1))).Value);

        return Assert.IsType<StartCheckoutResult.Started>((await start.ExecuteAsync(new StartCheckoutCommand(cart.CartId, customerId))).Value).SessionId;
    }
}