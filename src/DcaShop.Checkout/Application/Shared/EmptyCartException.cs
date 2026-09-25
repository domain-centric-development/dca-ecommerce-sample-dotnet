using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when a checkout would start from a cart that holds nothing.</summary>
/// <remarks>
/// Read before the session exists, so the customer is told to put something in the cart rather than meeting
/// the same rule one step later, where the model states it for the session itself.
/// </remarks>
public sealed class EmptyCartException : UseCaseException
{
    public EmptyCartException(CartId cartId)
        : base($"Cannot checkout empty cart: {cartId.Value}")
    {
        CartId = cartId;
    }

    public CartId CartId { get; }
}