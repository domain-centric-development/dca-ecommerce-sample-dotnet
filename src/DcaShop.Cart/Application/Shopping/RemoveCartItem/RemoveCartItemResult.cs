using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Cart.Application.Shopping.RemoveCartItem;

/// <summary>What taking an item out of the caller's active cart came to: removed, or one of the refusals a caller answers.</summary>
/// <remarks>
/// The command names no cart — the caller's active cart is meant — so there is no "cart not found" of its own: without
/// an active cart, the item the caller named is not in it.
/// </remarks>
[GenerateOneOf]
public sealed partial class RemoveCartItemResult
    : OneOfBase<RemoveCartItemResult.Removed, RemoveCartItemResult.CartItemNotFound, RemoveCartItemResult.CartNotModifiable>
{
    /// <summary>The item was removed.</summary>
    public sealed record Removed(Guid CartId, int ItemCount);

    /// <summary>The caller's active cart holds no item with that id — the aggregate's rule, or no active cart at all.</summary>
    public sealed record CartItemNotFound(CartItemId ItemId) : IRefusal;

    /// <summary>The cart is no longer active and gives no item up — the aggregate's rule.</summary>
    /// <param name="CartId">The cart.</param>
    /// <param name="Status">The status that refuses the change.</param>
    public sealed record CartNotModifiable(CartId CartId, CartStatus Status) : IRefusal;
}