using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Inventory.Events;

/// <summary>
/// Consumer-defined contract (interface inversion): Inventory reduces stock when an integration event carrying
/// this shape arrives. Checkout's <c>CheckoutConfirmedEvent</c> implements it; Inventory never depends on Checkout.
/// </summary>
public interface IStockReductionTrigger
{
    IReadOnlyList<StockReductionLineItem> OrderLineItems { get; }
}

/// <summary>One line of a confirmed order: how much of which product left the shop.</summary>
public sealed record StockReductionLineItem(ProductId ProductId, int Quantity);
