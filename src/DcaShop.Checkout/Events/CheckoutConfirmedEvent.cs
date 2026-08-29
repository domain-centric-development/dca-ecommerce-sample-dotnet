using DcaShop.Cart.Events;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Events;

/// <summary>
/// Published language of Checkout: an order was confirmed. Implements the cart's consumer-defined
/// <see cref="ICartCompletionTrigger"/> so the cart completes without depending on Checkout.
/// </summary>
[IntegrationEventType("checkout-confirmed", Version = 1)]
public sealed record CheckoutConfirmedEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    string SessionId,
    string CartId,
    string CustomerId,
    Money TotalAmount,
    IReadOnlyList<CheckoutConfirmedEvent.LineItemInfo> Items) : IIntegrationEvent, ICartCompletionTrigger
{
    public sealed record LineItemInfo(ProductId ProductId, int Quantity);
}
