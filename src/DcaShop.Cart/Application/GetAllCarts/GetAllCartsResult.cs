namespace DcaShop.Cart.Application.GetAllCarts;

/// <summary>
/// Every cart in the shop, as an operator sees them. The summaries carry the prices captured at addition rather
/// than current article data: a list across all customers would otherwise ask the Product context once per cart.
/// </summary>
public sealed record GetAllCartsResult(IReadOnlyList<GetAllCartsResult.CartSummary> Carts)
{
    public sealed record CartSummary(
        Guid CartId,
        string CustomerId,
        string Status,
        int ItemCount,
        decimal TotalAmount,
        string TotalCurrency);
}
