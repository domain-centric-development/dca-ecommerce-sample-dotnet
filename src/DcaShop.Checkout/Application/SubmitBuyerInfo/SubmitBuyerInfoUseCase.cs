using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.SubmitBuyerInfo;

public sealed class SubmitBuyerInfoUseCase : ISubmitBuyerInfoInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly IDomainEventPublisher _events;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitBuyerInfoUseCase(ICheckoutSessionRepository sessions, IDomainEventPublisher events, IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _sessions = sessions;
        _events = events;
    }

    public async Task<SubmitBuyerInfoResult> ExecuteAsync(SubmitBuyerInfoCommand command, CancellationToken cancellationToken = default)
    {
        // Whole use case is local: one short unit of work
        return await _unitOfWork.RunAsync(
            async ct =>
            {
                var session = await _sessions.FindByIdAsync(new CheckoutSessionId(command.SessionId), ct).ConfigureAwait(false)
                              ?? throw new ArgumentException($"Session not found: {command.SessionId}", nameof(command));

                session.SubmitBuyerInfo(new BuyerInfo(command.Email, command.FirstName, command.LastName, command.Phone));

                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);

                return new SubmitBuyerInfoResult(CheckoutSessionData.From(session));
            },
            cancellationToken).ConfigureAwait(false);
    }
}
