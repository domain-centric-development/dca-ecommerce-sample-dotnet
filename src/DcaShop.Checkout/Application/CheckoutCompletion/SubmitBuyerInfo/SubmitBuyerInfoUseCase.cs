using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;

public sealed class SubmitBuyerInfoUseCase : ISubmitBuyerInfoInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public SubmitBuyerInfoUseCase(ICheckoutSessionRepository sessions, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _sessions = sessions;
        _events = events;
    }

    public async Task<SubmitBuyerInfoResult> ExecuteAsync(SubmitBuyerInfoCommand command, CancellationToken cancellationToken = default)
    {
        var sessionId = new CheckoutSessionId(command.SessionId);

        // Whole use case is local: one short transaction
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                // The caller's own session: one that is not theirs is not found
                var session = await _sessions.FindByIdForCustomerAsync(sessionId, CustomerId.Of(command.CustomerId), ct).ConfigureAwait(false)
                              ?? throw new CheckoutSessionNotFoundException(sessionId);

                session.SubmitBuyerInfo(new BuyerInfo(command.Email, command.FirstName, command.LastName, command.Phone));

                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);

                return SubmitBuyerInfoResult.From(session);
            },
            cancellationToken).ConfigureAwait(false);
    }
}