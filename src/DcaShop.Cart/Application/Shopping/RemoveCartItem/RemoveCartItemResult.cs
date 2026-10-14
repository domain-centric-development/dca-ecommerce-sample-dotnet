namespace DcaShop.Cart.Application.Shopping.RemoveCartItem;

public sealed record RemoveCartItemResult(Guid CartId, int ItemCount);