namespace DcaShop.Cart.Application.Shopping.RemoveItemFromCart;

/// <summary>Removes the line holding <paramref name="ProductId"/> from one of <paramref name="CustomerId"/>'s carts.</summary>
public sealed record RemoveItemFromCartCommand(Guid CartId, string CustomerId, Guid ProductId);