namespace DcaShop.Cart.Application.Shopping.RemoveItemFromCart;

public sealed record RemoveItemFromCartResult(Guid CartId, int ItemCount, string Total);
