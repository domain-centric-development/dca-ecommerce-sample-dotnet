using DcaShop.Cart.Domain.Model;
using DcaShop.Cart.Domain.Service;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Cart.Application.Shopping.GetCartById;

/// <summary>
/// The cart's amounts as the page shows them, assembled by the use case so that no adapter runs the domain's
/// calculations: subtotal at today's prices, subtotal at the prices the items were added with, their absolute difference and
/// the value-added tax contained in the current subtotal.
/// </summary>
public sealed record CartTotals(Money CurrentSubtotal, Money OriginalSubtotal, Money Difference, Money ContainedTax)
{
    public static CartTotals From(EnrichedCart cart, CartTotalCalculator calculator)
    {
        var current = cart.CurrentSubtotal;
        return new CartTotals(current, cart.OriginalSubtotal, cart.TotalPriceDifference, calculator.ContainedTax(current));
    }
}
