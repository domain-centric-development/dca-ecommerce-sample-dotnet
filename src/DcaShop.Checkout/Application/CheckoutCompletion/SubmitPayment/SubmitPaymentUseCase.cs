using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;

/// <summary>
/// The payment step: the customer chooses how to pay, and the checkout moves on to the review. No payment is requested
/// here — "Place Order" has the chosen provider authorize the total the review showed.
/// </summary>
public sealed class SubmitPaymentUseCase : ISubmitPaymentInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly IPaymentProviderRegistry _providers;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public SubmitPaymentUseCase(ICheckoutSessionRepository sessions, IPaymentProviderRegistry providers, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _sessions = sessions;
        _providers = providers;
        _events = events;
    }

    public async Task<SubmitPaymentResult> ExecuteAsync(SubmitPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var sessionId = new CheckoutSessionId(command.SessionId);
        var customerId = CustomerId.Of(command.CustomerId);
        var providerId = PaymentProviderId.Of(command.PaymentProviderId);

        // Only a way to pay the shop offers and that takes payments right now is chosen; the registry may leave the
        // process, so it is asked before the transaction
        var provider = await _providers.FindAsync(providerId, cancellationToken).ConfigureAwait(false);
        if (provider is null)
        {
            return new SubmitPaymentResult.PaymentProviderNotFound(providerId);
        }

        if (!await provider.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            return new SubmitPaymentResult.PaymentProviderUnavailable(providerId);
        }

        return await _transactionBoundary.InTransactionAsync<SubmitPaymentResult>(
            async ct =>
            {
                var session = await _sessions.FindByIdForCustomerAsync(sessionId, customerId, ct).ConfigureAwait(false);
                if (session is null)
                {
                    return new SubmitPaymentResult.CheckoutSessionNotFound(sessionId);
                }

                try
                {
                    session.SubmitPayment(new PaymentSelection(providerId));
                }
                catch (CheckoutNotModifiableException notModifiable)
                {
                    return new SubmitPaymentResult.CheckoutNotModifiable(notModifiable.SessionId, notModifiable.Status);
                }
                catch (CheckoutStepNotCompletedException notCompleted)
                {
                    return new SubmitPaymentResult.CheckoutStepNotCompleted(notCompleted.SessionId, notCompleted.Step);
                }
                catch (CheckoutStepOutOfOrderException outOfOrder)
                {
                    return new SubmitPaymentResult.CheckoutStepOutOfOrder(outOfOrder.SessionId, outOfOrder.Requested, outOfOrder.Current);
                }
                catch (NothingToPayException nothing)
                {
                    return new SubmitPaymentResult.NothingToPay(nothing.SessionId, nothing.Total);
                }

                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                return SubmitPaymentResult.Submitted.From(session);
            },
            cancellationToken).ConfigureAwait(false);
    }
}