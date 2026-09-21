using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Raised when a step is missing the data a later step depends on.</summary>
/// <remarks>
/// Each step of the checkout contributes facts the next ones need — where to deliver, who buys, how it is
/// paid. A step that was skipped leaves those facts absent, and the session says which one.
/// </remarks>
public sealed class CheckoutStepNotCompletedException : DomainException
{
    public CheckoutStepNotCompletedException(CheckoutSessionId sessionId, CheckoutStep step)
        : base($"Step {step} must be completed first")
    {
        SessionId = sessionId;
        Step = step;
    }

    public CheckoutSessionId SessionId { get; }

    /// <summary>The step whose data is missing.</summary>
    public CheckoutStep Step { get; }
}
