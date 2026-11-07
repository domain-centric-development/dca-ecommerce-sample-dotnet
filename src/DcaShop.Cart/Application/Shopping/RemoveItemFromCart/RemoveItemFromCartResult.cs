using DcaShop.Cart.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Cart.Application.Shopping.RemoveItemFromCart;

/// <summary>What taking a product's item out of a cart came to: removed, or one of the refusals a caller answers.</summary>
[GenerateOneOf]
public sealed partial class RemoveItemFromCartResult
    : OneOfBase<
        RemoveItemFromCartResult.Removed,
        RemoveItemFromCartResult.CartNotFound,
        RemoveItemFromCartResult.CartItemNotFound,
        RemoveItemFromCartResult.CartNotModifiable>
{
    /// <summary>The item was removed; the cart's figures after the removal.</summary>
    public sealed record Removed(Guid CartId, int ItemCount, Money Total);

    /// <summary>No cart of this customer under that id (ADR-007).</summary>
    public sealed record CartNotFound(CartId CartId) : IRefusal;

    /// <summary>The cart holds no item of that product — the aggregate's rule.</summary>
    public sealed record CartItemNotFound(ProductId ProductId) : IRefusal;

    /// <summary>The cart is no longer active and gives no item up — the aggregate's rule.</summary>
    /// <param name="CartId">The cart.</param>
    /// <param name="Status">The status that refuses the change.</param>
    public sealed record CartNotModifiable(CartId CartId, CartStatus Status) : IRefusal;
}