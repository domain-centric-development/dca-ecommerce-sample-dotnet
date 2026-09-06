namespace DcaShop.Cart.Application.Shopping.AddItemToCart;

public sealed record AddItemToCartResult(Guid CartId, int ItemCount, int TotalQuantity, string Total);
