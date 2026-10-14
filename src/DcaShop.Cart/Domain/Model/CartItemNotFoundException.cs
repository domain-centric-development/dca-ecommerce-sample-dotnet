using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>Raised when a position the caller names is not in the cart.</summary>
/// <remarks>
/// The cart is asked to change something it does not hold — by position identity or by product. That is a
/// statement about this cart's contents, which only the cart can make, so it is a rule of the model rather
/// than a failed lookup in a store.
/// </remarks>
public sealed class CartItemNotFoundException : DomainException
{
    private CartItemNotFoundException(string message, CartItemId? itemId, ProductId? productId)
        : base(message)
    {
        ItemId = itemId;
        ProductId = productId;
    }

    /// <summary>The position identity the caller used, when the position was named by it.</summary>
    public CartItemId? ItemId { get; }

    /// <summary>The product the caller used, when the position was named by the product it holds.</summary>
    public ProductId? ProductId { get; }

    /// <summary>The position was named by its own identity.</summary>
    public static CartItemNotFoundException ForItem(CartItemId itemId) =>
        new($"Cart item not found: {itemId.Value}", itemId, null);

    /// <summary>The position was named by the product it holds.</summary>
    public static CartItemNotFoundException ForProduct(ProductId productId) =>
        new($"Product not found in cart: {productId.Value}", null, productId);
}