using DcaShop.Cart.Application.CartCheckout.CompleteCart;
using DcaShop.Cart.Events;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.Extensions.Logging;

namespace DcaShop.Cart.Adapter.Incoming.Event.CartCheckout;

/// <summary>
/// Completes the cart when any integration event implementing <see cref="ICartCompletionTrigger"/> arrives. It
/// hands the event id on; the use case deduplicates a redelivery inside its transaction.
/// </summary>
public sealed class CartCompletionEventConsumer : TransactionalEventListener<ICartCompletionTrigger>
{
    private readonly ICompleteCartInputPort _completeCart;
    private readonly ILogger<CartCompletionEventConsumer> _logger;

    public CartCompletionEventConsumer(ICompleteCartInputPort completeCart, ILogger<CartCompletionEventConsumer> logger)
    {
        _completeCart = completeCart;
        _logger = logger;
    }

    protected override async Task OnAsync(ICartCompletionTrigger @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing cart {CartId} after checkout confirmation", @event.CartId);
        await _completeCart.ExecuteAsync(
            new CompleteCartCommand(Guid.Parse(@event.CartId), @event.SessionId, @event.PurchasedPositions, @event.EventId),
            cancellationToken).ConfigureAwait(false);
    }
}