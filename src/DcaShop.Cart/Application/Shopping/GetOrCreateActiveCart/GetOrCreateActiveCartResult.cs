namespace DcaShop.Cart.Application.GetOrCreateActiveCart;

public sealed record GetOrCreateActiveCartResult(Guid CartId, bool Created);
