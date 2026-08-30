using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Events;

/// <summary>
/// Published language of the cart: its contents changed. Consolidates the internal domain events
/// (item added, product removed, quantity changed, cart cleared) into one cross-context event, so
/// consumers never listen to the cart's own domain events.
/// </summary>
[IntegrationEventType("cart-contents-changed", Version = 1)]
public sealed record CartContentsChangedEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid CartId,
    CartContentsChangedEvent.ChangeType Change) : IIntegrationEvent
{
    /// <summary>What happened to the cart's contents.</summary>
    public enum ChangeType
    {
        ItemAdded,
        ItemRemoved,
        QuantityChanged,
        CartCleared,
    }
}
