using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;

/// <summary>What the step came to: the buyer was recorded, or one of the refusals the caller answers.</summary>
[GenerateOneOf]
public sealed partial class SubmitBuyerInfoResult
    : OneOfBase<
        SubmitBuyerInfoResult.Submitted,
        SubmitBuyerInfoResult.CheckoutSessionNotFound,
        SubmitBuyerInfoResult.CheckoutNotModifiable,
        SubmitBuyerInfoResult.CheckoutStepOutOfOrder>
{
    /// <summary>What the caller needs for its next step: the session and where it stands. The page itself comes from a session query.</summary>
    public sealed record Submitted(Guid SessionId, CheckoutStep CurrentStep, CheckoutSessionStatus Status)
    {
        public static Submitted From(CheckoutSession session) => new(session.Id.Value, session.CurrentStep, session.Status);
    }

    /// <summary>The caller has no checkout session under that id — it does not exist or is somebody else's (ADR-007).</summary>
    public sealed record CheckoutSessionNotFound(CheckoutSessionId SessionId) : IRefusal;

    /// <summary>The session is no longer open for changes — confirmed, superseded, abandoned or expired.</summary>
    public sealed record CheckoutNotModifiable(CheckoutSessionId SessionId, CheckoutSessionStatus Status) : IRefusal;

    /// <summary>The requested step lies beyond the step the session has reached.</summary>
    public sealed record CheckoutStepOutOfOrder(CheckoutSessionId SessionId, CheckoutStep Requested, CheckoutStep Current) : IRefusal;
}