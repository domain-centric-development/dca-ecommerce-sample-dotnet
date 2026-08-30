namespace DcaShop.Cart.Adapter.Incoming.Api;

public sealed record ShoppingCartDto(
    string CartId,
    string CustomerId,
    IReadOnlyList<CartItemDto> Items,
    string Status,
    decimal Total,
    string Currency,
    int ItemCount);
