namespace DcaShop.Cart.Application.AddItemToCart;

public sealed record AddItemToCartResult(Guid CartId, int ItemCount, int TotalQuantity, string Total);
