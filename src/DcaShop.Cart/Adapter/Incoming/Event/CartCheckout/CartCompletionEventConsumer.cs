using DcaShop.Cart.Application.CartCheckout.CompleteCart;
using DcaShop.Cart.Events;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.Extensions.Logging;

namespace DcaShop.Cart.Adapter.Incoming.Event.CartCheckout;

/// <summary>
/// Completes the cart when any integration event implementing <see cref="ICartCompletionTrigger"/> arrives. It
/// hands the event id on; the use case deduplicates a redelivery inside its transaction.
/// </summary>
/// <remarks>
/// An event has nobody to answer: a cart the use case does not find fails the listener, so the delivery is retried and
/// stays visible as failed, as any failed listener does.
/// </remarks>
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
        var result = await _completeCart.ExecuteAsync(
            new CompleteCartCommand(Guid.Parse(@event.CartId), @event.CustomerId, @event.SessionId, @event.PurchasedPositions, @event.EventId),
            cancellationToken).ConfigureAwait(false);
        result.Switch(
            completed => { },
            notFound => throw new InvalidOperationException($"No cart to complete: {notFound.CartId.Value}"));
    }
}