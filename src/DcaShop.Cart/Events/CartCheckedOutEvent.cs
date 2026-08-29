using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Events;

/// <summary>Published language of the cart: checkout was triggered for this cart.</summary>
[IntegrationEventType("cart-checked-out", Version = 1)]
public sealed record CartCheckedOutEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid CartId,
    string CustomerId,
    Money TotalAmount,
    IReadOnlyList<CartCheckedOutEvent.ItemInfo> Items) : IIntegrationEvent
{
    public sealed record ItemInfo(ProductId ProductId, int Quantity);
}
