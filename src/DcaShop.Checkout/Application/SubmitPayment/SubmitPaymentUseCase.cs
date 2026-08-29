using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.SubmitPayment;

public sealed class SubmitPaymentUseCase : ISubmitPaymentInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly IPaymentProviderRegistry _providers;
    private readonly IDomainEventPublisher _events;

    public SubmitPaymentUseCase(ICheckoutSessionRepository sessions, IPaymentProviderRegistry providers, IDomainEventPublisher events)
    {
        _sessions = sessions;
        _providers = providers;
        _events = events;
    }

    public async Task<SubmitPaymentResult> ExecuteAsync(SubmitPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.FindByIdAsync(new CheckoutSessionId(command.SessionId), cancellationToken).ConfigureAwait(false)
                      ?? throw new ArgumentException($"Session not found: {command.SessionId}", nameof(command));

        var providerId = PaymentProviderId.Of(command.PaymentProviderId);
        if (await _providers.FindAsync(providerId, cancellationToken).ConfigureAwait(false) is null)
        {
            throw new ArgumentException($"Unknown payment provider: {providerId}", nameof(command));
        }

        session.SubmitPayment(new PaymentSelection(providerId));

        await _sessions.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(session, cancellationToken).ConfigureAwait(false);

        return new SubmitPaymentResult(CheckoutSessionData.From(session));
    }
}
