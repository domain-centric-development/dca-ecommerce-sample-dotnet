namespace DcaShop.Cart.Application.RemoveItemFromCart;

public sealed record RemoveItemFromCartCommand(Guid CartId, Guid ItemId);
