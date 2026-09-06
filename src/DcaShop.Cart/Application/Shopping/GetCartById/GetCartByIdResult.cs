using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Application.GetCartById;

/// <summary><see cref="Cart"/> is null when no cart with the requested id exists.</summary>
public sealed record GetCartByIdResult(EnrichedCart? Cart)
{
    public bool Found => Cart is not null;
}
