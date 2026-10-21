using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Raised when "Place Order" meets an order that changed since the review showed it, in anything but a price.</summary>
/// <remarks>
/// Another tab of the same browser can change the checkout — the shipping, the address, the payment — after a review
/// was shown. The order placed is the one the customer saw, so a change since refuses it.
/// </remarks>
public sealed class OrderChangedSinceReviewException : DomainException
{
    public OrderChangedSinceReviewException(CheckoutSessionId sessionId)
        : base($"Checkout {sessionId.Value} changed since the review")
    {
        SessionId = sessionId;
    }

    public CheckoutSessionId SessionId { get; }
}