using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.SubmitBuyerInfo;

public sealed class SubmitBuyerInfoUseCase : ISubmitBuyerInfoInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly IDomainEventPublisher _events;

    public SubmitBuyerInfoUseCase(ICheckoutSessionRepository sessions, IDomainEventPublisher events)
    {
        _sessions = sessions;
        _events = events;
    }

    public async Task<SubmitBuyerInfoResult> ExecuteAsync(SubmitBuyerInfoCommand command, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.FindByIdAsync(new CheckoutSessionId(command.SessionId), cancellationToken).ConfigureAwait(false)
                      ?? throw new ArgumentException($"Session not found: {command.SessionId}", nameof(command));

        session.SubmitBuyerInfo(new BuyerInfo(command.Email, command.FirstName, command.LastName, command.Phone));

        await _sessions.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(session, cancellationToken).ConfigureAwait(false);

        return new SubmitBuyerInfoResult(CheckoutSessionData.From(session));
    }
}
