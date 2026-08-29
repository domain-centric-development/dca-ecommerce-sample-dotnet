using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Amounts of a checkout session: subtotal, shipping, tax and grand total.</summary>
public sealed record CheckoutTotals(Money Subtotal, Money Shipping, Money Tax, Money Total) : IValue
{
    public static CheckoutTotals Calculate(Money subtotal, Money shipping, Money tax) =>
        new(subtotal, shipping, tax, subtotal.Add(shipping).Add(tax));

    public static CheckoutTotals Zero(string currency) => new(Money.Zero(currency), Money.Zero(currency), Money.Zero(currency), Money.Zero(currency));

    public CheckoutTotals WithShipping(Money newShipping) => Calculate(Subtotal, newShipping, Tax);

    public CheckoutTotals WithTax(Money newTax) => Calculate(Subtotal, Shipping, newTax);
}
