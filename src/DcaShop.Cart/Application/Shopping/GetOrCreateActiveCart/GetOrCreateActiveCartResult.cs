namespace DcaShop.Cart.Application.Shopping.GetOrCreateActiveCart;

public sealed record GetOrCreateActiveCartResult(Guid CartId, bool Created);
