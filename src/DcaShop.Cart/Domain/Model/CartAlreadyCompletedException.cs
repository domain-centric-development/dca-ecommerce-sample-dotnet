using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Cart.Domain.Model;

/// <summary>Raised when a cart that is already completed would be completed again.</summary>
/// <remarks>
/// Completion is the step that hands the cart's contents to the confirmed order; doing it twice would claim a
/// second order for the same cart.
/// </remarks>
public sealed class CartAlreadyCompletedException : DomainException
{
    public CartAlreadyCompletedException(CartId cartId)
        : base($"Cart {cartId.Value} is already completed")
    {
        CartId = cartId;
    }

    public CartId CartId { get; }
}