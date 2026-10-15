using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
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

    /// <summary>How long releasing a payment at the provider may take.</summary>
    private static readonly TimeSpan CancellationDeadline = TimeSpan.FromSeconds(10);

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
                       ?? throw new PaymentProviderNotFoundException(providerId);

        if (!await provider.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new PaymentProviderUnavailableException(providerId);
        }

        // Everything the session itself can refuse is refused here, before the provider is reached: a payment
        // intent must not exist for a checkout that cannot accept it. The amount to charge is the session total
        // as it stands at this moment.
        var snapshot = await _sessions.FindByIdForCustomerAsync(sessionId, customerId, cancellationToken).ConfigureAwait(false)
                       ?? throw new CheckoutSessionNotFoundException(sessionId);
        snapshot.AssertReadyForPayment();
        if (snapshot.IsTotalCoveredBy(providerId))
        {
            // The payment the provider authorized already pays this total: a second request would leave two open
            return SubmitPaymentResult.From(snapshot) with { PaymentAlreadyAuthorized = true };
        }

        var amount = snapshot.Totals.Total;
        await ReleaseReplacedPaymentAsync(snapshot, customerId, cancellationToken).ConfigureAwait(false);
        var providerReference = await RequestPaymentAsync(provider, providerId, sessionId, amount, cancellationToken).ConfigureAwait(false);

        // Short transaction: load, submit, save, publish. The session can still have moved on between the check
        // above and this load — a concurrent confirmation, an expiry — so the intent that is already at the
        // provider is released rather than left dangling.
        try
        {
            return await _transactionBoundary.InTransactionAsync(
                async ct =>
                {
                    var session = await _sessions.FindByIdForCustomerAsync(sessionId, customerId, ct).ConfigureAwait(false)
                                  ?? throw new CheckoutSessionNotFoundException(sessionId);
                    session.SubmitPayment(new PaymentSelection(providerId, providerReference, amount));
                    await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                    await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                    return SubmitPaymentResult.From(session);
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await CancelQuietlyAsync(provider, providerReference).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Asks the provider to open a payment of <paramref name="amount"/> for the session and answers its reference.</summary>
    private async Task<string> RequestPaymentAsync(IPaymentProvider provider, PaymentProviderId providerId, CheckoutSessionId sessionId, Money amount, CancellationToken cancellationToken)
    {
        var initiation = await provider.InitiatePaymentAsync(sessionId, amount, cancellationToken).ConfigureAwait(false);
        if (initiation.Outcome == IPaymentProvider.PaymentOutcome.Unavailable)
        {
            _logger.LogWarning("Payment provider {ProviderId} is unavailable: {Reason}", providerId, initiation.ErrorMessage);
            throw new PaymentProviderUnavailableException(providerId);
        }

        if (!initiation.Success || initiation.ProviderReference is not { } providerReference)
        {
            throw new PaymentInitiationFailedException(
                providerId,
                initiation.ErrorMessage ?? $"Payment provider {providerId} did not open a payment");
        }

        return providerReference;
    }

    /// <summary>
    /// A payment authorized for another total, or through another provider, no longer pays this checkout: it is released
    /// before the new one is requested, so the provider never holds two for one checkout. A released payment is
    /// forgotten at once, so it covers nothing even when the new request fails.
    /// </summary>
    private async Task ReleaseReplacedPaymentAsync(CheckoutSession snapshot, CustomerId customerId, CancellationToken cancellationToken)
    {
        if (snapshot.PaymentSelection is { HasReference: true } replaced
            && await ReleaseAsync(replaced, cancellationToken).ConfigureAwait(false))
        {
            await ForgetReleasedPaymentAsync(snapshot.Id, customerId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Releases the payment the session held through the provider that authorized it, where the shop still knows it;
    /// answers whether the provider released it.
    /// </summary>
    private async Task<bool> ReleaseAsync(PaymentSelection replaced, CancellationToken cancellationToken)
    {
        if (await _providers.FindAsync(replaced.ProviderId, cancellationToken).ConfigureAwait(false) is not { } provider)
        {
            _logger.LogWarning("Payment {Reference} could not be released: provider {ProviderId} is not known", replaced.ProviderReference, replaced.ProviderId);
            return false;
        }

        return await CancelQuietlyAsync(provider, replaced.ProviderReference!).ConfigureAwait(false);
    }

    /// <summary>Short transaction of its own: the session forgets the payment the provider released.</summary>
    private Task ForgetReleasedPaymentAsync(CheckoutSessionId sessionId, CustomerId customerId, CancellationToken cancellationToken) =>
        _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var session = await _sessions.FindByIdForCustomerAsync(sessionId, customerId, ct).ConfigureAwait(false)
                              ?? throw new CheckoutSessionNotFoundException(sessionId);
                session.ReleasePayment();
                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                return session;
            },
            cancellationToken);

    /// <summary>
    /// Releases a payment intent the session could not accept, or one a new payment replaces, and answers whether the
    /// provider released it. A provider that refuses the cancellation leaves the payment going on as it would have: the
    /// caller is told why their payment was rejected, or gets the new one, not that the clean-up failed.
    /// </summary>
    private async Task<bool> CancelQuietlyAsync(IPaymentProvider provider, string providerReference)
    {
        // Not the caller's token: a cancelled request is one of the reasons the transaction failed, and passing
        // that same token on would abort the clean-up before it reaches the provider — leaving behind exactly the
        // intent this exists to release. Its own deadline bounds it instead.
        using var deadline = new CancellationTokenSource(CancellationDeadline);

        try
        {
            var cancellation = await provider.CancelPaymentAsync(providerReference, deadline.Token).ConfigureAwait(false);
            if (!cancellation.Success)
            {
                _logger.LogWarning("Payment intent {Reference} could not be released: {Reason}", providerReference, cancellation.ErrorMessage);
            }

            return cancellation.Success;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Payment intent {Reference} could not be released", providerReference);
            return false;
        }
    }
}