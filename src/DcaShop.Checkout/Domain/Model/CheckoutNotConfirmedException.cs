using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Raised when a checkout would be completed although it was never confirmed.</summary>
/// <remarks>
/// Completion records that the order left the shop. Only a confirmed session has an order to record, so any
/// other status refuses.
/// </remarks>
public sealed class CheckoutNotConfirmedException : DomainException
{
    public CheckoutNotConfirmedException(CheckoutSessionId sessionId, CheckoutSessionStatus status)
        : base($"Cannot complete checkout {sessionId.Value} with status: {status}")
    {
        SessionId = sessionId;
        Status = status;
    }

    public CheckoutSessionId SessionId { get; }

    public CheckoutSessionStatus Status { get; }
}
