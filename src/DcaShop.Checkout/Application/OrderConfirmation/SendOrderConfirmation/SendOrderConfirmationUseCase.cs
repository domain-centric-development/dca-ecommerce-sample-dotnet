using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.OrderConfirmation.SendOrderConfirmation;

/// <summary>
/// Sends the order confirmation of a confirmed checkout: to the buyer e-mail of the checkout, with the order total it
/// was confirmed at, under one key per order. A delivery of the same event that comes again finds the effect recorded
/// and sends nothing; a crash between sending and recording sends again under the same key, which the provider sends
/// once. A provider that does not accept the mail is refused as unavailable, so the delivery is tried again later.
/// </summary>
public sealed class SendOrderConfirmationUseCase : ISendOrderConfirmationInputPort
{
    /// <summary>The effect's key in <see cref="IProcessedEvents"/> — stable: a rename would replay old events.</summary>
    private const string Effect = "send-order-confirmation";

    private readonly ICheckoutSessionRepository _sessions;
    private readonly IProcessedEvents _processedEvents;
    private readonly IOrderConfirmationMailPort _mail;

    public SendOrderConfirmationUseCase(ICheckoutSessionRepository sessions, IProcessedEvents processedEvents, IOrderConfirmationMailPort mail)
    {
        _sessions = sessions;
        _processedEvents = processedEvents;
        _mail = mail;
    }

    public async Task<SendOrderConfirmationResult> ExecuteAsync(SendOrderConfirmationCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await _processedEvents.ContainsAsync(Effect, command.EventId, cancellationToken).ConfigureAwait(false))
        {
            return new SendOrderConfirmationResult(command.SessionId, AlreadySent: true);
        }

        var sessionId = new CheckoutSessionId(command.SessionId);
        var session = await _sessions.FindByIdForCustomerAsync(sessionId, CustomerId.Of(command.CustomerId), cancellationToken).ConfigureAwait(false)
                      ?? throw new CheckoutSessionNotFoundException(sessionId);
        var buyer = session.BuyerInfo ?? throw new InvalidOperationException($"Confirmed checkout {sessionId} has no buyer");

        var mail = new IOrderConfirmationMailPort.OrderConfirmationMail(buyer.Email, session.Totals.Total, $"order-confirmation-{sessionId}");
        if (!await _mail.SendAsync(mail, cancellationToken).ConfigureAwait(false))
        {
            throw new MailProviderUnavailableException(mail.Key);
        }

        await _processedEvents.MarkProcessedAsync(Effect, command.EventId, cancellationToken).ConfigureAwait(false);
        return new SendOrderConfirmationResult(command.SessionId, AlreadySent: false);
    }
}