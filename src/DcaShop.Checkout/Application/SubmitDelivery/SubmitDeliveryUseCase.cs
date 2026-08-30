using DcaShop.Checkout.Domain.ReadModel;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.SubmitDelivery;

public sealed class SubmitDeliveryUseCase : ISubmitDeliveryInputPort
{
    private readonly ICheckoutSessionRepository _sessions;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public SubmitDeliveryUseCase(ICheckoutSessionRepository sessions, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _transactionBoundary = transactionBoundary;
        _sessions = sessions;
        _events = events;
    }

    public async Task<SubmitDeliveryResult> ExecuteAsync(SubmitDeliveryCommand command, CancellationToken cancellationToken = default)
    {
        // Whole use case is local: one short transaction
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                var session = await _sessions.FindByIdAsync(new CheckoutSessionId(command.SessionId), ct).ConfigureAwait(false)
                              ?? throw new ArgumentException($"Session not found: {command.SessionId}", nameof(command));

                var shippingOption = ShippingOptions.Find(command.ShippingOptionId)
                                     ?? throw new ArgumentException($"Unknown shipping option: {command.ShippingOptionId}", nameof(command));
                var address = new DeliveryAddress(command.Street, command.StreetLine2, command.City, command.PostalCode, command.Country, command.State);
                session.SubmitDelivery(address, shippingOption);

                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);

                return new SubmitDeliveryResult(CheckoutCartSnapshot.From(session));
            },
            cancellationToken).ConfigureAwait(false);
    }
}
