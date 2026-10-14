namespace DcaShop.Cart.Application.Shopping.RemoveCartItem;

/// <summary>Removes the item <paramref name="CartItemId"/> from <paramref name="CustomerId"/>'s active cart, whatever its quantity.</summary>
public sealed record RemoveCartItemCommand(string CustomerId, Guid CartItemId);