using DcaShop.SharedKernel.Domain.Model;
namespace DcaShop.Cart.Application.CartRecovery.GetCartMergeOptions;

/// <summary>
/// The two carts, when and only when the visitor has to choose between them. A merge is required only if both
/// carts hold items — with one of them empty there is nothing to decide. A take-over is required when only the guest
/// cart holds items: there is nothing to decide either, but the guest's lines still have to move into the account.
/// </summary>
public sealed record GetCartMergeOptionsResult(
    bool MergeRequired,
    GetCartMergeOptionsResult.CartSummary? AnonymousCart,
    GetCartMergeOptionsResult.CartSummary? AccountCart,
    bool TakeOverRequired = false)
{
    public static GetCartMergeOptionsResult NoMergeRequired() => new(false, null, null);

    public static GetCartMergeOptionsResult TakeOver() => new(false, null, null, TakeOverRequired: true);

    public static GetCartMergeOptionsResult Required(CartSummary anonymousCart, CartSummary accountCart) =>
        new(true, anonymousCart, accountCart);

    /// <summary>One of the two carts, enriched with names and images so the visitor can tell them apart.</summary>
    public sealed record CartSummary(
        Guid CartId,
        int ItemCount,
        int TotalQuantity,
        Money Total,
        IReadOnlyList<CartItemSummary> Items);

    public sealed record CartItemSummary(
        Guid ProductId, string ProductName, string? ImageUrl, int Quantity, Money UnitPrice);
}