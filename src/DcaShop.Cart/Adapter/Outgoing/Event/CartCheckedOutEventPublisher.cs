using DcaShop.Cart.Domain.Event;
using DcaShop.Cart.Events;
using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Adapter.Outgoing.Event;

/// <summary>Translates the domain event <see cref="CartCheckedOut"/> into the published <see cref="CartCheckedOutEvent"/>.</summary>
public sealed class CartCheckedOutEventPublisher : EventListener<CartCheckedOut>
{
    private readonly IIntegrationEventPublisher _publisher;

    public CartCheckedOutEventPublisher(IIntegrationEventPublisher publisher)
    {
        _publisher = publisher;
    }

    protected override Task OnAsync(CartCheckedOut @event, CancellationToken cancellationToken) =>
        _publisher.PublishAsync(
            new CartCheckedOutEvent(
                @event.EventId,
                @event.OccurredOn,
                @event.CartId.Value,
                @event.CustomerId.Value,
                @event.TotalAmount,
                @event.Items.Select(i => new CartCheckedOutEvent.ItemInfo(i.ProductId, i.Quantity)).ToList()),
            cancellationToken);
}
