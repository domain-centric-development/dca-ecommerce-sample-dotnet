namespace DcaShop.Cart.Application.MergeCarts;

/// <summary>The account's cart after the merge, and what the merge did.</summary>
public sealed record MergeCartsResult(
    Guid CartId,
    string CustomerId,
    IReadOnlyList<MergeCartsResult.CartItemSummary> Items,
    string Total,
    CartMergeStrategy StrategyApplied,
    int ItemsFromAnonymous,
    int ItemsFromAccount,
    bool AnonymousCartDeleted)
{
    public sealed record CartItemSummary(Guid ItemId, Guid ProductId, int Quantity, string UnitPrice);
}
