using DcaShop.Cart.Application.CartCheckout.CompleteCart;
using DcaShop.Cart.Application.Shared;
using DcaShop.Cart.Events;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.Extensions.Logging;

namespace DcaShop.Cart.Adapter.Incoming.Event.CartCheckout;

/// <summary>
/// Completes the cart when any integration event implementing <see cref="ICartCompletionTrigger"/> arrives.
/// Delivery is at least once: an event id already handled is skipped (<see cref="IProcessedEvents"/>).
/// </summary>
public sealed class CartCompletionEventConsumer : EventListener<ICartCompletionTrigger>
{
    private readonly ICompleteCartInputPort _completeCart;
    private readonly IProcessedEvents _processedEvents;
    private readonly ILogger<CartCompletionEventConsumer> _logger;

    public CartCompletionEventConsumer(ICompleteCartInputPort completeCart, IProcessedEvents processedEvents, ILogger<CartCompletionEventConsumer> logger)
    {
        _completeCart = completeCart;
        _processedEvents = processedEvents;
        _logger = logger;
    }

    protected override async Task OnAsync(ICartCompletionTrigger @event, CancellationToken cancellationToken)
    {
        if (await _processedEvents.ContainsAsync(@event.EventId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Skipping event {EventId}: already handled", @event.EventId);
            return;
        }

        _logger.LogInformation("Completing cart {CartId} after checkout confirmation", @event.CartId);
        await _completeCart.ExecuteAsync(new CompleteCartCommand(Guid.Parse(@event.CartId), @event.SessionId, @event.PurchasedPositions), cancellationToken).ConfigureAwait(false);
        await _processedEvents.MarkProcessedAsync(@event.EventId, cancellationToken).ConfigureAwait(false);
    }
}