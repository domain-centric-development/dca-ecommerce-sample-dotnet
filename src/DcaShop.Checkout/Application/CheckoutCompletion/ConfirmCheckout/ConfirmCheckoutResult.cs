using DcaShop.Checkout.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;

/// <summary>
/// What placing the order came to: confirmed, or one of the refusals the review answers. Eleven cases — more than
/// OneOf's own bases hold, so this one stands on <c>OneOf.Extended</c>.
/// </summary>
[GenerateOneOf]
public sealed partial class ConfirmCheckoutResult
    : OneOfBase<
        ConfirmCheckoutResult.Confirmed,
        ConfirmCheckoutResult.CheckoutSessionNotFound,
        ConfirmCheckoutResult.CheckoutNotModifiable,
        ConfirmCheckoutResult.CheckoutStepNotCompleted,
        ConfirmCheckoutResult.CheckoutStepOutOfOrder,
        ConfirmCheckoutResult.CheckoutValidation,
        ConfirmCheckoutResult.PriceChangedSinceReview,
        ConfirmCheckoutResult.OrderChangedSinceReview,
        ConfirmCheckoutResult.PaymentProviderNotFound,
        ConfirmCheckoutResult.PaymentProviderUnavailable,
        ConfirmCheckoutResult.PaymentInitiationFailed>
{
    /// <summary>What the caller needs for its next step: the session and where it stands. The page itself comes from a session query.</summary>
    public sealed record Confirmed(Guid SessionId, CheckoutStep CurrentStep, CheckoutSessionStatus Status)
    {
        public static Confirmed From(CheckoutSession session) => new(session.Id.Value, session.CurrentStep, session.Status);
    }

    /// <summary>The caller has no checkout session under that id — it does not exist or is somebody else's (ADR-007).</summary>
    public sealed record CheckoutSessionNotFound(CheckoutSessionId SessionId) : IRefusal;

    /// <summary>The session is no longer open for changes — confirmed in another tab, superseded, abandoned or expired.</summary>
    public sealed record CheckoutNotModifiable(CheckoutSessionId SessionId, CheckoutSessionStatus Status) : IRefusal;

    /// <summary>An earlier step has not been completed yet.</summary>
    public sealed record CheckoutStepNotCompleted(CheckoutSessionId SessionId, CheckoutStep Step) : IRefusal;

    /// <summary>The session has not reached the review.</summary>
    public sealed record CheckoutStepOutOfOrder(CheckoutSessionId SessionId, CheckoutStep Requested, CheckoutStep Current) : IRefusal;

    /// <summary>Lines are unavailable or short of stock at this moment — the stock reservation's shortage among them.</summary>
    public sealed record CheckoutValidation(CheckoutValidationResult Validation) : IRefusal;

    /// <summary>A line's price changed since the review; the line is named by its product.</summary>
    public sealed record PriceChangedSinceReview(CheckoutSessionId SessionId, string ProductName) : IRefusal;

    /// <summary>The order changed since the review in another way than a line's price.</summary>
    public sealed record OrderChangedSinceReview(CheckoutSessionId SessionId) : IRefusal;

    /// <summary>The shop no longer offers the way to pay the customer chose.</summary>
    public sealed record PaymentProviderNotFound(PaymentProviderId ProviderId) : IRefusal;

    /// <summary>The way to pay is not taking payments or did not answer.</summary>
    public sealed record PaymentProviderUnavailable(PaymentProviderId ProviderId) : IRefusal;

    /// <summary>The provider refused the payment; <paramref name="Reason"/> is the provider's, for the log only.</summary>
    public sealed record PaymentInitiationFailed(PaymentProviderId ProviderId, string Reason) : IRefusal;
}