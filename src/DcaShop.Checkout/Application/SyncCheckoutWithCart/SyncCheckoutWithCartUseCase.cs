using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.Extensions.Logging;

namespace DcaShop.Checkout.Application.SyncCheckoutWithCart;

/// <summary>
/// Rebuilds the line items of the active checkout session from the current cart. Prices are the ones the customer
/// saw when adding the item; names and images come from the catalog.
/// </summary>
public sealed class SyncCheckoutWithCartUseCase : ISyncCheckoutWithCartInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly ICartDataPort _cartData;
    private readonly TaxCalculator _taxCalculator;
    private readonly ICheckoutArticleDataPort _articleData;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;
    private readonly ILogger<SyncCheckoutWithCartUseCase> _logger;

    public SyncCheckoutWithCartUseCase(
        ICheckoutSessionRepository sessions,
        ICartDataPort cartData,
        TaxCalculator taxCalculator,
        ICheckoutArticleDataPort articleData,
        IDomainEventPublisher events,
        ITransactionBoundary transactionBoundary,
        ILogger<SyncCheckoutWithCartUseCase> logger)
    {
        _sessions = sessions;
        _cartData = cartData;
        _taxCalculator = taxCalculator;
        _articleData = articleData;
        _events = events;
        _transactionBoundary = transactionBoundary;
        _logger = logger;
    }

    public async Task<SyncCheckoutWithCartResult> ExecuteAsync(SyncCheckoutWithCartCommand command, CancellationToken cancellationToken = default)
    {
        var cartId = new CartId(command.CartId);
        var activeSession = await _sessions.FindActiveByCartIdAsync(cartId, cancellationToken).ConfigureAwait(false);
        if (activeSession is null)
        {
            _logger.LogDebug("No active checkout session for cart {CartId}, skipping sync", cartId);
            return SyncCheckoutWithCartResult.NoActiveSession();
        }

        var sessionId = activeSession.Id;

        // Cart and article data come from other contexts (remote-capable) — outside the transaction
        // No caller here — the session names the customer this runs on behalf of.
        var cart = await _cartData.FindByIdAsync(cartId, activeSession.CustomerId, cancellationToken).ConfigureAwait(false)
                   ?? throw new InvalidOperationException($"Cart not found for active session: {cartId}");
        if (cart.Items.Count == 0)
        {
            _logger.LogWarning("Cart {CartId} is empty but has active checkout session {SessionId}, skipping sync", cartId, sessionId);
            return SyncCheckoutWithCartResult.NoActiveSession();
        }

        var articles = await _articleData.GetArticleDataAsync(cart.Items.Select(i => i.ProductId).ToArray(), cancellationToken).ConfigureAwait(false);
        var newLineItems = new List<CheckoutLineItem>();
        var subtotal = Money.Euro(0m);
        foreach (var cartItem in cart.Items)
        {
            if (!articles.TryGetValue(cartItem.ProductId, out var article))
            {
                throw new ArgumentException($"Product not found: {cartItem.ProductId}", nameof(command));
            }

            var lineItem = new CheckoutLineItem(CheckoutLineItemId.Generate(), cartItem.ProductId, article.Name, cartItem.PriceAtAddition, cartItem.Quantity, article.ImageUrl);
            newLineItems.Add(lineItem);
            subtotal = subtotal.Add(lineItem.LineTotal);
        }

        // Short transaction: reload, sync, save, publish
        var result = await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var session = await _sessions.FindByIdAsync(sessionId, ct).ConfigureAwait(false)
                              ?? throw new InvalidOperationException($"Checkout session vanished: {sessionId}");
                session.SyncLineItems(newLineItems, subtotal, _taxCalculator);
                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                return SyncCheckoutWithCartResult.Synced(session.Id.Value, newLineItems.Count);
            },
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Synced checkout session {SessionId} with cart {CartId} — {ItemCount} items, subtotal {Subtotal}", sessionId, cartId, newLineItems.Count, subtotal);
        return result;
    }
}
