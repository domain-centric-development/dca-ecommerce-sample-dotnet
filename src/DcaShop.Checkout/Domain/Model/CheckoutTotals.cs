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
    public static CheckoutTotals Calculate(Money subtotal, Money shipping, Money tax) =>
        new(subtotal, shipping, tax, subtotal.Add(shipping));

    public static CheckoutTotals Zero(string currency) => new(Money.Zero(currency), Money.Zero(currency), Money.Zero(currency), Money.Zero(currency));

    public CheckoutTotals WithShipping(Money newShipping) => Calculate(Subtotal, newShipping, Tax);

    public CheckoutTotals WithTax(Money newTax) => Calculate(Subtotal, Shipping, newTax);
}
