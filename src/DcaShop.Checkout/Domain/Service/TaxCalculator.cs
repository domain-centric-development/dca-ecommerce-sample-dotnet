using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Service;

/// <summary>
/// The value-added tax contained in a checkout's totals. Line-item prices and shipping costs are gross
/// amounts, so the tax is extracted rather than added: the grand total stays subtotal plus shipping, and the
/// tax line tells the customer how much of it goes to the tax authority.
/// </summary>
/// <remarks>
/// The Cart context owns the same rule for its own page. Duplicating a rule this small keeps the two
/// contexts independent — neither has to change when the other's presentation does.
/// </remarks>
public sealed class TaxCalculator : IDomainService
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
