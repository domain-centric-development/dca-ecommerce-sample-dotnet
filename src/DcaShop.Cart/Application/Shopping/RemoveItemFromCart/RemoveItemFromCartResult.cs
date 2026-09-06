namespace DcaShop.Cart.Application.RemoveItemFromCart;

public sealed record RemoveItemFromCartResult(Guid CartId, int ItemCount, string Total);
