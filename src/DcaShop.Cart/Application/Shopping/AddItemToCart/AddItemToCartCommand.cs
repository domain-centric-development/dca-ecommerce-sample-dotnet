namespace DcaShop.Cart.Application.Shopping.AddItemToCart;

/// <summary>Adds a product to one of <paramref name="CustomerId"/>'s carts.</summary>
public sealed record AddItemToCartCommand(Guid CartId, string CustomerId, Guid ProductId, int Quantity);
