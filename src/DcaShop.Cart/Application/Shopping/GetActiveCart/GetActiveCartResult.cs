using DcaShop.Cart.Application.Shopping.GetCartById;
using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Application.Shopping.GetActiveCart;

/// <summary>
/// <see cref="Cart"/> is null when the customer has no active cart. <see cref="Totals"/> carries the cart's amounts —
/// business facts the use case assembles so that no page has to compute them.
/// </summary>
public sealed record GetActiveCartResult(EnrichedCart? Cart, CartTotals? Totals = null)
{
    public bool Found => Cart is not null;
}