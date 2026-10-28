using DcaShop.Checkout.Application.OrderConfirmation.SendOrderConfirmation;
using DcaShop.Checkout.Domain.Event;
using DcaShop.SharedKernel.Infrastructure.Events;

namespace DcaShop.Checkout.Adapter.Incoming.Event.OrderConfirmation;

/// <summary>
/// Sends the order confirmation once a checkout is confirmed: after the confirming transaction committed, from the
/// outbox, which tries again with backoff while the mail provider is unavailable.
/// </summary>
public sealed class CheckoutConfirmedEventConsumer : TransactionalEventListener<CheckoutConfirmed>
{
    private readonly ISendOrderConfirmationInputPort _sendOrderConfirmation;

    public CheckoutConfirmedEventConsumer(ISendOrderConfirmationInputPort sendOrderConfirmation)
    {
        _sendOrderConfirmation = sendOrderConfirmation;
    }

    protected override Task OnAsync(CheckoutConfirmed @event, CancellationToken cancellationToken) =>
        _sendOrderConfirmation.ExecuteAsync(
            new SendOrderConfirmationCommand(@event.EventId, @event.SessionId.Value, @event.CustomerId.Value),
            cancellationToken);
}