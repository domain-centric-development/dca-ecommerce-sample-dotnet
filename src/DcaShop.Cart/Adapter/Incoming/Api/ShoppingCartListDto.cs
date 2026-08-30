namespace DcaShop.Cart.Adapter.Incoming.Api;

/// <summary>Every cart in the shop, as the operator listing returns them.</summary>
public sealed record ShoppingCartListDto(IReadOnlyList<ShoppingCartListDto.CartSummaryDto> Carts)
{
    public sealed record CartSummaryDto(
        string CartId,
        string CustomerId,
        string Status,
        int ItemCount,
        decimal Total,
        string Currency);
}
