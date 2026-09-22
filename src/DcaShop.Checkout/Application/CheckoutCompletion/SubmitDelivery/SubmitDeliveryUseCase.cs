using DomainCentric.BuildingBlocks.Application.Transactions;
using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitDelivery;

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
        var sessionId = new CheckoutSessionId(command.SessionId);

        // Whole use case is local: one short transaction
        return await _transactionBoundary.InTransactionAsync(
            async ct =>
            {
                // The caller's own session: one that is not theirs is not found
                var session = await _sessions.FindByIdForCustomerAsync(sessionId, CustomerId.Of(command.CustomerId), ct).ConfigureAwait(false)
                              ?? throw new CheckoutSessionNotFoundException(sessionId);

                var shippingOption = ShippingOptions.Find(command.ShippingOptionId)
                                     ?? throw new ArgumentException($"Unknown shipping option: {command.ShippingOptionId}", nameof(command));
                var address = new DeliveryAddress(command.Street, command.StreetLine2, command.City, command.PostalCode, command.Country, command.State);
                session.SubmitDelivery(address, shippingOption);

                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);

                return SubmitDeliveryResult.From(session);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
