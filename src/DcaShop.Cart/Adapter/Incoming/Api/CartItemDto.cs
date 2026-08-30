namespace DcaShop.Cart.Adapter.Incoming.Api;

public sealed record CartItemDto(
    string ItemId,
    string ProductId,
    int Quantity,
    decimal Price,
    string Currency);
