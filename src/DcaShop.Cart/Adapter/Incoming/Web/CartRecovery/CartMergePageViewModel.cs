using DcaShop.Cart.Application.GetCartMergeOptions;

namespace DcaShop.Cart.Adapter.Incoming.Web;

/// <summary>
/// What the merge page shows: both carts side by side, plus the two values the decision has to carry back —
/// which identity the guest cart belongs to and where the visitor was headed.
/// </summary>
public sealed record CartMergePageViewModel(
    CartMergePageViewModel.CartSummaryViewModel AnonymousCart,
    CartMergePageViewModel.CartSummaryViewModel AccountCart,
    string AnonymousUserId,
    string? ReturnUrl)
{
    public static CartMergePageViewModel FromResult(
        GetCartMergeOptionsResult result, string anonymousUserId, string? returnUrl)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.AnonymousCart is null || result.AccountCart is null)
        {
            throw new ArgumentException("The merge page needs both carts", nameof(result));
        }

        return new CartMergePageViewModel(
            CartSummaryViewModel.From(result.AnonymousCart),
            CartSummaryViewModel.From(result.AccountCart),
            anonymousUserId,
            returnUrl);
    }

    public sealed record CartSummaryViewModel(
        Guid CartId,
        int ItemCount,
        int TotalQuantity,
        string Total,
        IReadOnlyList<CartItemViewModel> Items)
    {
        internal static CartSummaryViewModel From(GetCartMergeOptionsResult.CartSummary summary) =>
            new(
                summary.CartId,
                summary.ItemCount,
                summary.TotalQuantity,
                summary.Total,
                summary.Items.Select(CartItemViewModel.From).ToList());
    }

    public sealed record CartItemViewModel(
        Guid ProductId, string ProductName, string? ImageUrl, int Quantity, string UnitPrice)
    {
        internal static CartItemViewModel From(GetCartMergeOptionsResult.CartItemSummary item) =>
            new(item.ProductId, item.ProductName, item.ImageUrl, item.Quantity, item.UnitPrice);
    }
}
