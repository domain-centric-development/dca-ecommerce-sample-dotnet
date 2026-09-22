using System;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>Read model of a cart with current article data: price-change detection, checkout eligibility, subtotals.</summary>
public sealed record EnrichedCart(CartId CartId, CustomerId CustomerId, CartStatus Status, IReadOnlyList<EnrichedCartItem> Items) : IValue
{
    public int ItemCount => Items.Count;

    public int TotalQuantity => Items.Sum(i => i.Quantity.Value);

    public bool IsEmpty => Items.Count == 0;

    /// <summary>VAT contained in the gross prices this context works with.</summary>
    private const decimal Rate = 0.19m;

    /// <summary>
    /// The tax contained in the cart's current subtotal, at the cart's rate. Prices are gross, so the tax is
    /// <em>contained</em> in the amount rather than added to it. The rule is the cart's own: it taxes goods,
    /// while the checkout taxes goods and shipping, so the two contexts state it separately.
    /// </summary>
    public Money ContainedTax() => ContainedTax(Rate);

    /// <summary>The tax contained in the current subtotal at the given rate.</summary>
    public Money ContainedTax(decimal taxRate)
    {
        if (taxRate < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate cannot be negative");
        }

        var gross = CurrentSubtotal;
        var net = gross.Amount / (1m + taxRate);
        return Money.Of(gross.Amount - net, gross.Currency);
    }

    public Money CurrentSubtotal => Items.Aggregate(Money.Euro(0m), (sum, i) => sum.Add(i.CurrentLineTotal));

    public Money OriginalSubtotal => Items.Aggregate(Money.Euro(0m), (sum, i) => sum.Add(i.OriginalLineTotal));

    /// <summary>Absolute difference between the current and the original subtotal — prices may have dropped.</summary>
    public Money TotalPriceDifference =>
        CurrentSubtotal.IsGreaterThan(OriginalSubtotal) ? CurrentSubtotal.Subtract(OriginalSubtotal) : OriginalSubtotal.Subtract(CurrentSubtotal);

    public bool HasAnyPriceChanges => Items.Any(i => i.HasPriceChanged);

    public bool IsValidForCheckout => Status == CartStatus.Active && !IsEmpty && Items.All(i => i.IsValidForCheckout);
}
