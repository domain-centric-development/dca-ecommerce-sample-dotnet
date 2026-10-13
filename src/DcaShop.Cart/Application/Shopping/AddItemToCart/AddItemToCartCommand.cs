namespace DcaShop.Cart.Application.Shopping.AddItemToCart;

/// <summary>Adds a product to one of <paramref name="CustomerId"/>'s carts.</summary>
/// <param name="ProductId">The product as the caller named it — not necessarily an id any product has.</param>
public sealed record AddItemToCartCommand(Guid CartId, string CustomerId, string ProductId, int Quantity);