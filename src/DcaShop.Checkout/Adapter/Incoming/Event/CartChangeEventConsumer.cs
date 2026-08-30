using DcaShop.Cart.Events;
using DcaShop.Checkout.Application.SyncCheckoutWithCart;
using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace DcaShop.Checkout.Adapter.Incoming.Event;

/// <summary>
/// Keeps an active checkout session in step with its cart: every published cart change re-syncs the session.
/// A cleared cart is only logged — the session keeps its items until the customer acts.
/// </summary>
public sealed class CartChangeEventConsumer : EventListener<CartContentsChangedEvent>
{
    private readonly ISyncCheckoutWithCartInputPort _syncCheckoutWithCart;
    private readonly ILogger<CartChangeEventConsumer> _logger;

    public CartChangeEventConsumer(ISyncCheckoutWithCartInputPort syncCheckoutWithCart, ILogger<CartChangeEventConsumer> logger)
    {
        _syncCheckoutWithCart = syncCheckoutWithCart;
        _logger = logger;
    }

    protected override async Task OnAsync(CartContentsChangedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.Change == CartContentsChangedEvent.ChangeType.CartCleared)
        {
            _logger.LogWarning("Cart {CartId} was cleared during checkout — the checkout session keeps stale data", @event.CartId);
            return;
        }

        _logger.LogDebug("Cart contents changed [{Change}] for cart {CartId}, syncing checkout session", @event.Change, @event.CartId);
        var result = await _syncCheckoutWithCart.ExecuteAsync(new SyncCheckoutWithCartCommand(@event.CartId), cancellationToken).ConfigureAwait(false);
        if (result.WasSynced)
        {
            _logger.LogInformation("Checkout session {SessionId} synced with cart {CartId} — {ItemCount} items", result.SessionId, @event.CartId, result.ItemCount);
        }
    }
}
