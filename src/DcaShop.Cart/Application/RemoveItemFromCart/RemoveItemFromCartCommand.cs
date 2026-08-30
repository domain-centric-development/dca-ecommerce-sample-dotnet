namespace DcaShop.Cart.Application.RemoveItemFromCart;

/// <summary>Removes a line from one of <paramref name="CustomerId"/>'s carts.</summary>
public sealed record RemoveItemFromCartCommand(Guid CartId, string CustomerId, Guid ItemId);
