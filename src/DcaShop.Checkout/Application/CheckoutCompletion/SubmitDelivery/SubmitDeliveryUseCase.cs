using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.Service;

using DomainCentric.BuildingBlocks.Application.Transactions;
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
        return await _transactionBoundary.InTransactionAsync<SubmitDeliveryResult>(
            async ct =>
            {
                // The caller's own session: one that is not theirs is not found
                var session = await _sessions.FindByIdForCustomerAsync(sessionId, CustomerId.Of(command.CustomerId), ct).ConfigureAwait(false);
                if (session is null)
                {
                    return new SubmitDeliveryResult.CheckoutSessionNotFound(sessionId);
                }

                if (ShippingOptions.Find(command.ShippingOptionId) is not { } shippingOption)
                {
                    return new SubmitDeliveryResult.ShippingOptionNotOffered(command.ShippingOptionId);
                }

                var address = new DeliveryAddress(command.Street, command.StreetLine2, command.City, command.PostalCode, command.Country, command.State);
                try
                {
                    session.SubmitDelivery(address, shippingOption);
                }
                catch (CheckoutNotModifiableException notModifiable)
                {
                    return new SubmitDeliveryResult.CheckoutNotModifiable(notModifiable.SessionId, notModifiable.Status);
                }
                catch (CheckoutStepNotCompletedException notCompleted)
                {
                    return new SubmitDeliveryResult.CheckoutStepNotCompleted(notCompleted.SessionId, notCompleted.Step);
                }
                catch (CheckoutStepOutOfOrderException outOfOrder)
                {
                    return new SubmitDeliveryResult.CheckoutStepOutOfOrder(outOfOrder.SessionId, outOfOrder.Requested, outOfOrder.Current);
                }

                await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
                await _events.PublishAndClearEventsAsync(session, ct).ConfigureAwait(false);

                return SubmitDeliveryResult.Submitted.From(session);
            },
            cancellationToken).ConfigureAwait(false);
    }
}