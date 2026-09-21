using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Raised when the checkout would be navigated to a step nobody navigates to.</summary>
/// <remarks>
/// The confirmation step is reached by confirming, never by asking for it: it exists to show what happened, so
/// arriving there without the act it reports would show a purchase that was never made.
/// </remarks>
public sealed class CheckoutStepNotNavigableException : DomainException
{
    public CheckoutStepNotNavigableException(CheckoutSessionId sessionId, CheckoutStep step)
        : base($"Cannot navigate directly to step {step}")
    {
        SessionId = sessionId;
        Step = step;
    }

    public CheckoutSessionId SessionId { get; }

    public CheckoutStep Step { get; }
}
