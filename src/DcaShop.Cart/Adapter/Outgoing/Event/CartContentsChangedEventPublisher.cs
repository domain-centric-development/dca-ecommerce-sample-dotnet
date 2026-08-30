using DcaShop.Cart.Domain.Event;
using DcaShop.Cart.Events;
using DcaShop.SharedKernel.Infrastructure.Events;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Cart.Adapter.Outgoing.Event;

/// <summary>
/// Translates the four content-changing domain events into the single published
/// <see cref="CartContentsChangedEvent"/>. Listening on several event types at once is why this
/// adapter implements <see cref="IEventListener"/> directly instead of deriving from
/// <see cref="EventListener{TEvent}"/>.
/// </summary>
public sealed class CartContentsChangedEventPublisher : IEventListener
{
    private readonly IIntegrationEventPublisher _publisher;

    public CartContentsChangedEventPublisher(IIntegrationEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public bool Listens(object @event) => Describe(@event) is not null;

    public Task OnAsync(object @event, CancellationToken cancellationToken = default)
    {
        if (Describe(@event) is not { } change)
        {
            return Task.CompletedTask;
        }

        return _publisher.PublishAsync(
            new CartContentsChangedEvent(change.EventId, change.OccurredOn, change.CartId, change.Change),
            cancellationToken);
    }

    private static (Guid EventId, DateTimeOffset OccurredOn, Guid CartId, CartContentsChangedEvent.ChangeType Change)? Describe(object @event) => @event switch
    {
        CartItemAddedToCart e => (e.EventId, e.OccurredOn, e.CartId.Value, CartContentsChangedEvent.ChangeType.ItemAdded),
        ProductRemovedFromCart e => (e.EventId, e.OccurredOn, e.CartId.Value, CartContentsChangedEvent.ChangeType.ItemRemoved),
        CartItemQuantityChanged e => (e.EventId, e.OccurredOn, e.CartId.Value, CartContentsChangedEvent.ChangeType.QuantityChanged),
        CartCleared e => (e.EventId, e.OccurredOn, e.CartId.Value, CartContentsChangedEvent.ChangeType.CartCleared),
        _ => null,
    };
}
