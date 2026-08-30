namespace DcaShop.Cart.Application.GetCartMergeOptions;

/// <summary>
/// The two carts, when and only when the visitor has to choose between them. A merge is required only if both
/// carts hold items — with one of them empty there is nothing to decide.
/// </summary>
public sealed record GetCartMergeOptionsResult(
    bool MergeRequired,
    GetCartMergeOptionsResult.CartSummary? AnonymousCart,
    GetCartMergeOptionsResult.CartSummary? AccountCart)
{
    public static GetCartMergeOptionsResult NoMergeRequired() => new(false, null, null);

    public static GetCartMergeOptionsResult Required(CartSummary anonymousCart, CartSummary accountCart) =>
        new(true, anonymousCart, accountCart);

    /// <summary>One of the two carts, enriched with names and images so the visitor can tell them apart.</summary>
    public sealed record CartSummary(
        Guid CartId,
        int ItemCount,
        int TotalQuantity,
        string Total,
        IReadOnlyList<CartItemSummary> Items);

    public sealed record CartItemSummary(
        Guid ProductId, string ProductName, string? ImageUrl, int Quantity, string UnitPrice);
}
