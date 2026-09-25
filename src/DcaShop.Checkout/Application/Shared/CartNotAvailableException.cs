using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when the cart a checkout would start from is not available to the asking customer.</summary>
/// <remarks>
/// Checkout does not own the cart; it reads it through its own port. A cart that is missing and a cart that
/// belongs to somebody else are reported alike, for the same reason the session lookup does it (ADR-007).
/// </remarks>
public sealed class CartNotAvailableException : UseCaseException
{
    public CartNotAvailableException(CartId cartId)
        : base($"Cart not found: {cartId.Value}")
    {
        CartId = cartId;
    }

    public CartId CartId { get; }
}