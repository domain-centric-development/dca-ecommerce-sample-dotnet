namespace DcaShop.Cart.Application.AddItemToCart;

public sealed record AddItemToCartCommand(Guid CartId, Guid ProductId, int Quantity);
