using DcaShop.Checkout.Domain.ReadModel;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.StartCheckout;

/// <summary>Starts a checkout from an active cart. Line items get fresh prices; the cart stays active until confirmation.</summary>
public sealed class StartCheckoutUseCase : IStartCheckoutInputPort
{
    private readonly ICartDataPort _cartData;
    private readonly CheckoutCartFactory _checkoutCartFactory;
    private readonly ICheckoutArticleDataPort _articleData;
    private readonly ICheckoutSessionRepository _sessions;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public StartCheckoutUseCase(ICartDataPort cartData, CheckoutCartFactory checkoutCartFactory, ICheckoutArticleDataPort articleData, ICheckoutSessionRepository sessions, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _cartData = cartData;
        _checkoutCartFactory = checkoutCartFactory;
        _articleData = articleData;
        _sessions = sessions;
        _events = events;
    }

    public async Task<StartCheckoutResult> ExecuteAsync(StartCheckoutCommand command, CancellationToken cancellationToken = default)
    {
        var cartId = new CartId(command.CartId);
        var existing = await _sessions.FindActiveByCartIdAsync(cartId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return new StartCheckoutResult(CheckoutCartSnapshot.From(existing));
        }

        // Cart and article data come from other contexts (remote-capable) — outside the transaction
        var cart = await _cartData.FindByIdAsync(cartId, cancellationToken).ConfigureAwait(false)
                   ?? throw new ArgumentException($"Cart not found: {cartId}", nameof(command));
        if (!cart.Active)
        {
            throw new InvalidOperationException($"Cart is not active: {cartId}");
        }

        if (cart.Items.Count == 0)
        {
            throw new InvalidOperationException($"Cannot checkout empty cart: {cartId}");
        }

        var articles = await _articleData.GetArticleDataAsync(cart.Items.Select(i => i.ProductId).ToArray(), cancellationToken).ConfigureAwait(false);
        var lineItems = new List<CheckoutLineItem>();
        foreach (var cartItem in cart.Items)
        {
            if (!articles.TryGetValue(cartItem.ProductId, out var article))
            {
                throw new ArgumentException($"Product not found: {cartItem.ProductId}", nameof(command));
            }

            lineItems.Add(new CheckoutLineItem(CheckoutLineItemId.Generate(), cartItem.ProductId, article.Name, article.CurrentPrice, cartItem.Quantity, article.ImageUrl));
        }

        // The enriched read model pairs each line item with its current article data, so the domain can
        // answer availability, stock and pricing questions before a session exists
        var checkoutCart = _checkoutCartFactory.Create(cart.CartId, cart.CustomerId, lineItems, articles);
        if (!checkoutCart.IsValidForCheckout)
        {
            throw new InvalidOperationException($"Cannot start checkout, {checkoutCart.InvalidItems().Count} item(s) unavailable or out of stock");
        }

        var subtotal = checkoutCart.CalculateCurrentSubtotal();

        // Short transaction: create, save, publish
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var session = CheckoutSession.Start(cart.CartId, cart.CustomerId, lineItems, subtotal);
                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                return new StartCheckoutResult(CheckoutCartSnapshot.From(session));
            },
            cancellationToken).ConfigureAwait(false);
    }
}
