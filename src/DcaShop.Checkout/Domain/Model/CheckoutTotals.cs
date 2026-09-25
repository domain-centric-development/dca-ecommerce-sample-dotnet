using System;

using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>
/// Amounts of a checkout session: subtotal, shipping, the contained tax and the grand total. Prices are
/// gross prices — <c>Tax</c> is the share of subtotal and shipping that is value-added tax, not an extra
/// charge, which is why <c>Total</c> is subtotal plus shipping and does not add it a second time.
/// </summary>
public sealed record CheckoutTotals(Money Subtotal, Money Shipping, Money Tax, Money Total) : IValue
{
    /// <summary>VAT contained in the gross amounts this context works with.</summary>
    private const decimal Rate = 0.19m;

    /// <summary>
    /// Totals for goods and shipping, with the contained tax derived from them at the context's rate.
    /// The rule is the checkout's own: prices are gross, so the tax is <em>contained</em> in the amount
    /// rather than added to it, and the grand total does not change when it is worked out. A context that
    /// taxes a different basis — the cart taxes goods only — states its own rule in its own type.
    /// </summary>
    public static CheckoutTotals Calculate(Money subtotal, Money shipping) =>
        Calculate(subtotal, shipping, ContainedTax(subtotal.Add(shipping)));

    /// <summary>The tax contained in a gross amount at the checkout's rate.</summary>
    public static Money ContainedTax(Money grossAmount) => ContainedTax(grossAmount, Rate);

    /// <summary>The tax contained in a gross amount at the given rate.</summary>
    public static Money ContainedTax(Money grossAmount, decimal taxRate)
    {
        ArgumentNullException.ThrowIfNull(grossAmount);
        if (taxRate < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate cannot be negative");
        }

        var net = grossAmount.Amount / (1m + taxRate);
        return Money.Of(grossAmount.Amount - net, grossAmount.Currency);
    }

    public static CheckoutTotals Calculate(Money subtotal, Money shipping, Money tax) =>
        new(subtotal, shipping, tax, subtotal.Add(shipping));

    public static CheckoutTotals Zero(string currency) => new(Money.Zero(currency), Money.Zero(currency), Money.Zero(currency), Money.Zero(currency));

    /// <summary>Shipping changes the basis, so the contained tax is worked out again.</summary>
    public CheckoutTotals WithShipping(Money newShipping) => Calculate(Subtotal, newShipping);

    public CheckoutTotals WithTax(Money newTax) => Calculate(Subtotal, Shipping, newTax);
}