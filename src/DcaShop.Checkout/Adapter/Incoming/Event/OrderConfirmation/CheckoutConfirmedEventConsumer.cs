using DcaShop.Checkout.Application.OrderConfirmation.SendOrderConfirmation;
using DcaShop.Checkout.Domain.Event;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.Extensions.Logging;

namespace DcaShop.Checkout.Adapter.Incoming.Event.OrderConfirmation;

/// <summary>
/// Sends the order confirmation once a checkout is confirmed: after the confirming transaction committed, from the
/// outbox, which tries again with backoff while the mail provider is unavailable.
/// </summary>
/// <remarks>
/// The result maps onto the outbox: a sent confirmation completes the delivery; a checkout that holds no placed order is
/// parked — logged and completed, since trying again changes nothing; an unavailable mail provider has nobody here to
/// answer it, so the case is thrown back, which fails the delivery for the outbox's retry.
/// </remarks>
public sealed class CheckoutConfirmedEventConsumer : TransactionalEventListener<CheckoutConfirmed>
{
    private readonly ISendOrderConfirmationInputPort _sendOrderConfirmation;
    private readonly ILogger<CheckoutConfirmedEventConsumer> _logger;

    public CheckoutConfirmedEventConsumer(ISendOrderConfirmationInputPort sendOrderConfirmation, ILogger<CheckoutConfirmedEventConsumer> logger)
    {
        _sendOrderConfirmation = sendOrderConfirmation;
        _logger = logger;
    }

    protected override async Task OnAsync(CheckoutConfirmed @event, CancellationToken cancellationToken)
    {
        var result = await _sendOrderConfirmation.ExecuteAsync(
            new SendOrderConfirmationCommand(@event.EventId, @event.SessionId.Value, @event.CustomerId.Value),
            cancellationToken).ConfigureAwait(false);
        result.Switch(
            sent => { },
            noPlacedOrder => _logger.LogWarning("No order confirmation for checkout session {SessionId}: it holds no placed order", noPlacedOrder.SessionId),
            unavailable => throw new InvalidOperationException($"The mail provider did not accept the mail {unavailable.Key}"));
    }
}