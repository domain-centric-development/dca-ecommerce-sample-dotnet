using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Raised when "Place Order" meets a price that changed since the review showed the order.</summary>
/// <remarks>
/// The customer orders what the review showed them. A price that moved since is not ordered behind their back: the
/// review shows today's price, and the customer places the order again.
/// </remarks>
public sealed class PriceChangedSinceReviewException : DomainException
{
    public PriceChangedSinceReviewException(CheckoutSessionId sessionId, string productName)
        : base($"The price of '{productName}' in checkout {sessionId.Value} changed since the review")
    {
        SessionId = sessionId;
        ProductName = productName;
    }

    public CheckoutSessionId SessionId { get; }

    /// <summary>The product whose price changed.</summary>
    public string ProductName { get; }
}