using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Application.Shopping.GetCartById;

/// <summary>
/// <see cref="Cart"/> is null when no cart with the requested id exists. <see cref="Totals"/> carries the cart's amounts
/// — business facts the use case assembles so that no page has to compute them.
/// </summary>
public sealed record GetCartByIdResult(EnrichedCart? Cart, CartTotals? Totals)
{
    public bool Found => Cart is not null;

    public static GetCartByIdResult NotFound() => new(null, null);
}
