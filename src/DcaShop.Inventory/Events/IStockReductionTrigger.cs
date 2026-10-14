using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Inventory.Events;

/// <summary>
/// Consumer-defined contract (interface inversion): Inventory reduces stock when an integration event carrying
/// this shape arrives. Checkout's <c>CheckoutConfirmedEvent</c> implements it; Inventory never depends on Checkout.
/// It extends <see cref="IIntegrationEvent"/>: delivery is at least once, so the consumer deduplicates on the
/// <see cref="IIntegrationEvent.EventId"/> the contract hands it.
/// </summary>
public interface IStockReductionTrigger : IIntegrationEvent
{
    IReadOnlyList<StockReductionLineItem> LineItems { get; }
}

/// <summary>One line of a confirmed order: how much of which product left the shop.</summary>
public sealed record StockReductionLineItem(ProductId ProductId, int Quantity);