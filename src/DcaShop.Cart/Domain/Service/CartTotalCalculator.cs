using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Service;

/// <summary>
/// The value-added tax contained in a cart's amounts. Article prices are gross prices: what the customer sees
/// is what the customer pays, so the tax is not added on top — it is extracted, and the cart shows how much of
/// the subtotal is VAT while the subtotal itself stays as it is.
/// </summary>
/// <remarks>
/// The Checkout context owns the same rule for its own totals. Duplicating a rule this small keeps the two
/// contexts independent — neither has to change when the other's presentation does.
/// </remarks>
public sealed class CartTotalCalculator : IDomainService
{
    private const decimal DefaultTaxRate = 0.19m; // 19% VAT

    public Money ContainedTax(Money grossAmount, decimal taxRate)
    {
        ArgumentNullException.ThrowIfNull(grossAmount);
        if (taxRate < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate cannot be negative");
        }

        var net = grossAmount.Amount / (1m + taxRate);
        return Money.Of(grossAmount.Amount - net, grossAmount.Currency);
    }

    public Money ContainedTax(Money grossAmount) => ContainedTax(grossAmount, DefaultTaxRate);
}
