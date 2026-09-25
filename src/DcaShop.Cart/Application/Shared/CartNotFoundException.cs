using DcaShop.Cart.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

namespace DcaShop.Cart.Application.Shared;

/// <summary>Raised when the addressed cart is not available to the asking customer.</summary>
/// <remarks>
/// Deliberately one failure for two situations: the cart does not exist, and the cart belongs to somebody
/// else. Telling those apart would let a stranger probe which cart identities are real, so the lookup asks for
/// the cart <em>of this customer</em> and reports the same thing either way (ADR-007).
/// </remarks>
public sealed class CartNotFoundException : UseCaseException
{
    public CartNotFoundException(CartId cartId)
        : base($"Cart not found: {cartId.Value}")
    {
        CartId = cartId;
    }

    public CartId CartId { get; }
}