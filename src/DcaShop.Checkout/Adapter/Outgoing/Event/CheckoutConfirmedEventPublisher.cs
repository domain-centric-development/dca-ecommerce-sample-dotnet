using DcaShop.Checkout.Domain.Event;
using DcaShop.Checkout.Events;
using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Adapter.Outgoing.Event;

/// <summary>Translates the domain event <see cref="CheckoutConfirmed"/> into the published <see cref="CheckoutConfirmedEvent"/>.</summary>
public sealed class CheckoutConfirmedEventPublisher : EventListener<CheckoutConfirmed>
{
    private readonly IIntegrationEventPublisher _publisher;

    public CheckoutConfirmedEventPublisher(IIntegrationEventPublisher publisher)
    {
        _publisher = publisher;
    }

    protected override Task OnAsync(CheckoutConfirmed @event, CancellationToken cancellationToken) =>
        _publisher.PublishAsync(
            new CheckoutConfirmedEvent(
                @event.EventId,
                @event.OccurredOn,
                @event.SessionId.Value.ToString(),
                @event.CartId.Value.ToString(),
                @event.CustomerId.Value,
                @event.TotalAmount,
                @event.Items.Select(i => new CheckoutConfirmedEvent.LineItemInfo(i.ProductId, i.Quantity)).ToList()),
            cancellationToken);
}
