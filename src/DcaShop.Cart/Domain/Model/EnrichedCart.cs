using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>Read model of a cart with current article data: price-change detection, checkout eligibility, subtotals.</summary>
public sealed record EnrichedCart(CartId CartId, CustomerId CustomerId, CartStatus Status, IReadOnlyList<EnrichedCartItem> Items) : IValue
{
    public int ItemCount => Items.Count;

    public int TotalQuantity => Items.Sum(i => i.Quantity.Value);

    public bool IsEmpty => Items.Count == 0;

    public Money CurrentSubtotal => Items.Aggregate(Money.Euro(0m), (sum, i) => sum.Add(i.CurrentLineTotal));

    public Money OriginalSubtotal => Items.Aggregate(Money.Euro(0m), (sum, i) => sum.Add(i.OriginalLineTotal));

    public bool HasAnyPriceChanges => Items.Any(i => i.HasPriceChanged);

    public bool IsValidForCheckout => Status == CartStatus.Active && !IsEmpty && Items.All(i => i.IsValidForCheckout);
}
