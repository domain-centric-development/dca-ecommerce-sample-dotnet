using DomainCentric.BuildingBlocks.Application.Transactions;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.SubmitPayment;

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
        var providerId = PaymentProviderId.Of(command.PaymentProviderId);

        // Provider lookup is remote-capable (payment service provider) — outside the transaction
        if (await _providers.FindAsync(providerId, cancellationToken).ConfigureAwait(false) is null)
        {
            throw new ArgumentException($"Unknown payment provider: {providerId}", nameof(command));
        }

        // Short transaction: load, submit, save, publish
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var session = await _sessions.FindByIdAsync(sessionId, ct).ConfigureAwait(false)
                              ?? throw new ArgumentException($"Session not found: {command.SessionId}", nameof(command));
                session.SubmitPayment(new PaymentSelection(providerId, string.IsNullOrWhiteSpace(command.ProviderReference) ? null : command.ProviderReference.Trim()));
                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);
                return new SubmitPaymentResult(CheckoutSessionData.From(session));
            },
            cancellationToken).ConfigureAwait(false);
    }
}
