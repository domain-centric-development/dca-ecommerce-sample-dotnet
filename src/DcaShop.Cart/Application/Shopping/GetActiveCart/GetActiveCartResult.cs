using DcaShop.Cart.Domain.Model;

namespace DcaShop.Cart.Application.GetActiveCart;

public sealed record GetActiveCartResult(EnrichedCart? Cart)
{
    public bool Found => Cart is not null;
}
