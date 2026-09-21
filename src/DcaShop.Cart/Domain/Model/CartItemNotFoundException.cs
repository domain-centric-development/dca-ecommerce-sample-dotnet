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
    private CartItemNotFoundException(string message, string reference)
        : base(message)
    {
        Reference = reference;
    }

    /// <summary>The identity the caller used — a position identity or a product identity.</summary>
    public string Reference { get; }

    /// <summary>The position was named by its own identity.</summary>
    public static CartItemNotFoundException ForItem(CartItemId itemId) =>
        new($"Cart item not found: {itemId.Value}", itemId.Value.ToString());

    /// <summary>The position was named by the product it holds.</summary>
    public static CartItemNotFoundException ForProduct(ProductId productId) =>
        new($"Product not found in cart: {productId.Value}", productId.Value.ToString());
}
