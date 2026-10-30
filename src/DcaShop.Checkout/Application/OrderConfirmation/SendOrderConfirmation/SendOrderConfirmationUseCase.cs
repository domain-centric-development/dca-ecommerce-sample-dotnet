using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;

namespace DcaShop.Checkout.Application.OrderConfirmation.SendOrderConfirmation;

/// <summary>
/// Sends the order confirmation of a confirmed checkout: to the buyer e-mail of the checkout, with the order total it
/// was confirmed at, under one key per order. A delivery of the same event that comes again finds the effect recorded
/// and sends nothing. The mail leaves the process, so it is sent before the transaction that records the effect
/// (ADR-004): a crash between the two sends again under the same key, which the provider sends once. A provider that
/// does not accept the mail is refused as unavailable before anything is recorded, so the delivery is tried again later.
/// </summary>
public sealed class SendOrderConfirmationUseCase : ISendOrderConfirmationInputPort
{
    /// <summary>The effect's key in <see cref="IProcessedEvents"/> — stable: a rename would replay old events.</summary>
    private const string Effect = "send-order-confirmation";

    private readonly ICheckoutSessionRepository _sessions;
    private readonly IProcessedEvents _processedEvents;
    private readonly IOrderConfirmationMailer _mailer;
    private readonly ITransactionBoundary _transactionBoundary;

    public SendOrderConfirmationUseCase(
        ICheckoutSessionRepository sessions,
        IProcessedEvents processedEvents,
        IOrderConfirmationMailer mailer,
        ITransactionBoundary transactionBoundary)
    {
        _sessions = sessions;
        _processedEvents = processedEvents;
        _mailer = mailer;
        _transactionBoundary = transactionBoundary;
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
        var buyer = session.BuyerInfo ?? throw new BuyerInfoNotSubmittedException(sessionId);

        // The mail provider is remote: the mail goes out before the transaction, never inside it
        var mail = new IOrderConfirmationMailer.OrderConfirmationMail(buyer.Email, session.Totals.Total, MailKeyOf(sessionId));
        if (await _mailer.SendAsync(mail, cancellationToken).ConfigureAwait(false) is not IOrderConfirmationMailer.MailOutcome.Accepted)
        {
            throw new MailProviderUnavailableException(mail.Key);
        }

        // What is left is local: one short transaction that records the effect
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                await _processedEvents.MarkProcessedAsync(Effect, command.EventId, ct).ConfigureAwait(false);
                return new SendOrderConfirmationResult(command.SessionId, AlreadySent: false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The mail's key at the provider, one per order — stable: the provider sends one mail per key, so a renamed key
    /// would send every order's confirmation a second time.
    /// </summary>
    private static string MailKeyOf(CheckoutSessionId sessionId) => $"order-confirmation-{sessionId}";
}