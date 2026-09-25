using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Raised when a checkout session that is no longer open would be changed.</summary>
/// <remarks>
/// A session stops taking changes once it is confirmed, completed, abandoned or expired — from then on it is
/// the record of a purchase, not a form.
/// </remarks>
public sealed class CheckoutNotModifiableException : DomainException
{
    public CheckoutNotModifiableException(CheckoutSessionId sessionId, CheckoutSessionStatus status)
        : base($"Cannot modify checkout {sessionId.Value} with status: {status}")
    {
        SessionId = sessionId;
        Status = status;
    }

    public CheckoutSessionId SessionId { get; }

    /// <summary>The status that refuses the change.</summary>
    public CheckoutSessionStatus Status { get; }
}