using DcaShop.Cart.Events;
using DcaShop.Inventory.Events;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Events;

/// <summary>
/// Published language of Checkout: an order was confirmed. It implements the consumer-defined contracts of the
/// contexts that react to it — the cart's <see cref="ICartCompletionTrigger"/> and inventory's
/// <see cref="IStockReductionTrigger"/> — so neither of them depends on Checkout.
/// </summary>
[IntegrationEventType("checkout-confirmed", Version = 1)]
public sealed record CheckoutConfirmedEvent(
    Guid EventId,
    DateTimeOffset OccurredOn,
    string SessionId,
    string CartId,
    string CustomerId,
    Money TotalAmount,
    IReadOnlyList<CheckoutConfirmedEvent.LineItemInfo> Items) : IIntegrationEvent, ICartCompletionTrigger, IStockReductionTrigger
{
    public sealed record LineItemInfo(ProductId ProductId, int Quantity);

    /// <summary>The confirmed lines, in the shape Inventory defined for its stock reduction.</summary>
    public IReadOnlyList<StockReductionLineItem> OrderLineItems =>
        Items.Select(item => new StockReductionLineItem(item.ProductId, item.Quantity)).ToList();
}
