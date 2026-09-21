using DcaShop.Checkout.Domain.Model;
using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>Raised when a checkout would start from a cart that is no longer active.</summary>
/// <remarks>
/// A completed or abandoned cart has had its decision; starting a checkout from it would buy the contents of a
/// cart the customer has already left.
/// </remarks>
public sealed class CartNotActiveException : UseCaseException
{
    public CartNotActiveException(CartId cartId)
        : base($"Cart is not active: {cartId.Value}")
    {
        CartId = cartId;
    }

    public CartId CartId { get; }
}
