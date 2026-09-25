using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Raised when a checkout would start without a single line item.</summary>
/// <remarks>
/// A checkout is the act of buying what is in the cart; with nothing in it there is nothing to buy, and every
/// later step would compute totals over an empty list.
/// </remarks>
public sealed class EmptyCheckoutException : DomainException
{
    public EmptyCheckoutException(CartId cartId)
        : base($"Cannot start checkout for cart {cartId.Value} without line items")
    {
        CartId = cartId;
    }

    public CartId CartId { get; }
}