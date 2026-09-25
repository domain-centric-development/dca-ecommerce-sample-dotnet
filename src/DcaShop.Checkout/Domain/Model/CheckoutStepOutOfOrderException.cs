using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DcaShop.Checkout.Domain.Model;

/// <summary>Raised when the checkout would move to a step that is not reachable from where it stands.</summary>
/// <remarks>
/// The steps are an order, not a menu: a customer may return to an earlier one, but skipping ahead would
/// submit facts that depend on decisions not taken yet.
/// </remarks>
public sealed class CheckoutStepOutOfOrderException : DomainException
{
    public CheckoutStepOutOfOrderException(CheckoutSessionId sessionId, CheckoutStep requested, CheckoutStep current)
        : base($"Cannot move to step {requested} from {current}")
    {
        SessionId = sessionId;
        Requested = requested;
        Current = current;
    }

    public CheckoutSessionId SessionId { get; }

    /// <summary>The step the caller asked for.</summary>
    public CheckoutStep Requested { get; }

    /// <summary>The step the session stands at.</summary>
    public CheckoutStep Current { get; }
}