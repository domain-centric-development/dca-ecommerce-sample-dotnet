using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>Raised when a cart that is no longer active would be changed.</summary>
/// <remarks>
/// Only an active cart takes items, quantity changes or a merge. Once it is completed or abandoned its
/// contents are the record of what happened, and changing them would rewrite history.
/// </remarks>
public sealed class CartNotModifiableException : DomainException
{
    public CartNotModifiableException(CartId cartId, CartStatus status)
        : base($"Cannot modify cart {cartId.Value} with status: {status}")
    {
        CartId = cartId;
        Status = status;
    }

    public CartId CartId { get; }

    /// <summary>The status that refuses the change.</summary>
    public CartStatus Status { get; }
}