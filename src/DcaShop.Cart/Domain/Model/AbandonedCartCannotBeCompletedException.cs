using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>Raised when an abandoned cart would be completed.</summary>
/// <remarks>
/// Abandoning is the customer's decision that this cart is over. A later confirmation must start from a cart
/// the customer still holds, not revive the one they left.
/// </remarks>
public sealed class AbandonedCartCannotBeCompletedException : DomainException
{
    public AbandonedCartCannotBeCompletedException(CartId cartId)
        : base($"Cannot complete abandoned cart {cartId.Value}")
    {
        CartId = cartId;
    }

    public CartId CartId { get; }
}
