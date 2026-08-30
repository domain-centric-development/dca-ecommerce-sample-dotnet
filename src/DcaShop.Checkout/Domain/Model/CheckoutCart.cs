using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>
/// The cart as checkout sees it: enriched line items plus the questions the flow asks of them — running
/// totals, price drift since the items were added, and whether every item can still be bought.
/// </summary>
public sealed record CheckoutCart : IValue
{
    private const string DefaultCurrency = "EUR";

    public CheckoutCart(CartId cartId, CustomerId customerId, IReadOnlyList<EnrichedCheckoutLineItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        CartId = cartId;
        CustomerId = customerId;
        Items = items.ToList();
    }

    public CartId CartId { get; }

    public CustomerId CustomerId { get; }

    public IReadOnlyList<EnrichedCheckoutLineItem> Items { get; }

    public Money CalculateCurrentSubtotal() =>
        Items.Aggregate(Money.Zero(DefaultCurrency), (sum, item) => sum.Add(item.CurrentLineTotal));

    public Money CalculateOriginalSubtotal() =>
        Items.Aggregate(Money.Zero(DefaultCurrency), (sum, item) => sum.Add(item.OriginalLineTotal));

    public Money TotalPriceDifference()
    {
        var current = CalculateCurrentSubtotal();
        var original = CalculateOriginalSubtotal();
        return current.IsGreaterThan(original) ? current.Subtract(original) : original.Subtract(current);
    }

    public bool HasAnyPriceChanges => Items.Any(i => i.HasPriceChanged);

    public IReadOnlyList<EnrichedCheckoutLineItem> ItemsWithPriceChanges() => Items.Where(i => i.HasPriceChanged).ToList();

    public bool IsValidForCheckout => Items.Count > 0 && Items.All(i => i.IsValidForCheckout);

    public IReadOnlyList<EnrichedCheckoutLineItem> InvalidItems() => Items.Where(i => !i.IsValidForCheckout).ToList();

    public IReadOnlyList<EnrichedCheckoutLineItem> UnavailableItems() => Items.Where(i => !i.CurrentArticle.IsAvailable).ToList();

    public IReadOnlyList<EnrichedCheckoutLineItem> ItemsWithInsufficientStock() => Items.Where(i => !i.HasSufficientStock).ToList();

    public int ItemCount => Items.Count;

    public int TotalQuantity => Items.Sum(i => i.LineItem.Quantity);
}
