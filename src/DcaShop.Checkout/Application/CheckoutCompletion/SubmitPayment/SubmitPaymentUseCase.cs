using DomainCentric.BuildingBlocks.Application.Transactions;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.Extensions.Logging;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;

public sealed class SubmitPaymentUseCase : ISubmitPaymentInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly IPaymentProviderRegistry _providers;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;
    private readonly ILogger<SubmitPaymentUseCase> _logger;

    public SubmitPaymentUseCase(ICheckoutSessionRepository sessions, IPaymentProviderRegistry providers, IDomainEventPublisher events, ITransactionBoundary transactionBoundary, ILogger<SubmitPaymentUseCase> logger)
    {
        _transactionBoundary = transactionBoundary;
        _sessions = sessions;
        _providers = providers;
        _events = events;
        _logger = logger;
    }

    public async Task<SubmitPaymentResult> ExecuteAsync(SubmitPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var sessionId = new CheckoutSessionId(command.SessionId);
        var customerId = CustomerId.Of(command.CustomerId);
        var providerId = PaymentProviderId.Of(command.PaymentProviderId);

        // Provider lookup and payment initiation are remote-capable (payment service provider) —
        // both stay outside the transaction
        var provider = await _providers.FindAsync(providerId, cancellationToken).ConfigureAwait(false)
                       ?? throw new ArgumentException($"Unknown payment provider: {providerId}", nameof(command));

        if (!await provider.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Payment provider is currently unavailable: {providerId}");
        }

        // Everything the session itself can refuse is refused here, before the provider is reached: a payment
        // intent must not exist for a checkout that cannot accept it. The amount to charge is the session total
        // as it stands at this moment.
        var snapshot = await _sessions.FindByIdForCustomerAsync(sessionId, customerId, cancellationToken).ConfigureAwait(false)
                       ?? throw new ArgumentException($"Session not found: {command.SessionId}", nameof(command));
        snapshot.AssertReadyForPayment();
        var amount = snapshot.Totals.Total;

        var initiation = await provider.InitiatePaymentAsync(sessionId, amount, cancellationToken).ConfigureAwait(false);
        if (!initiation.Success || initiation.ProviderReference is not { } providerReference)
        {
            throw new InvalidOperationException(initiation.ErrorMessage);
        }

        // Short transaction: load, submit, save, publish. The session can still have moved on between the check
        // above and this load — a concurrent confirmation, an expiry — so the intent that is already at the
        // provider is released rather than left dangling.
        try
        {
            return await _transactionBoundary.InTransactionAsync(
                async ct =>
                {
                    var session = await _sessions.FindByIdForCustomerAsync(sessionId, customerId, ct).ConfigureAwait(false)
                                  ?? throw new ArgumentException($"Session not found: {command.SessionId}", nameof(command));
                    session.SubmitPayment(new PaymentSelection(providerId, providerReference));
                    await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                    await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                    return SubmitPaymentResult.From(session);
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await CancelQuietlyAsync(provider, providerReference, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Releases a payment intent the session could not accept. A provider that refuses the cancellation leaves the
    /// original failure standing: the caller is told why their payment was rejected, not that the clean-up of it
    /// failed as well.
    /// </summary>
    private async Task CancelQuietlyAsync(IPaymentProvider provider, string providerReference, CancellationToken cancellationToken)
    {
        try
        {
            var cancellation = await provider.CancelPaymentAsync(providerReference, cancellationToken).ConfigureAwait(false);
            if (!cancellation.Success)
            {
                _logger.LogWarning("Payment intent {Reference} could not be released: {Reason}", providerReference, cancellation.ErrorMessage);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Payment intent {Reference} could not be released", providerReference);
        }
    }
}
