using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application;

using OneOf;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;

/// <summary>What the step came to: the way to pay was recorded, or one of the refusals the caller answers.</summary>
[GenerateOneOf]
public sealed partial class SubmitPaymentResult
    : OneOfBase<
        SubmitPaymentResult.Submitted,
        SubmitPaymentResult.PaymentProviderNotFound,
        SubmitPaymentResult.PaymentProviderUnavailable,
        SubmitPaymentResult.CheckoutSessionNotFound,
        SubmitPaymentResult.CheckoutNotModifiable,
        SubmitPaymentResult.CheckoutStepNotCompleted,
        SubmitPaymentResult.CheckoutStepOutOfOrder,
        SubmitPaymentResult.NothingToPay>
{
    /// <summary>What the caller needs for its next step: the session and where it stands. The page itself comes from a session query.</summary>
    public sealed record Submitted(Guid SessionId, CheckoutStep CurrentStep, CheckoutSessionStatus Status)
    {
        public static Submitted From(CheckoutSession session) => new(session.Id.Value, session.CurrentStep, session.Status);
    }

    /// <summary>The shop offers no way to pay under that id.</summary>
    public sealed record PaymentProviderNotFound(PaymentProviderId ProviderId) : IRefusal;

    /// <summary>The way to pay is not taking payments right now.</summary>
    public sealed record PaymentProviderUnavailable(PaymentProviderId ProviderId) : IRefusal;

    /// <summary>The caller has no checkout session under that id — it does not exist or is somebody else's (ADR-007).</summary>
    public sealed record CheckoutSessionNotFound(CheckoutSessionId SessionId) : IRefusal;

    /// <summary>The session is no longer open for changes — confirmed, superseded, abandoned or expired.</summary>
    public sealed record CheckoutNotModifiable(CheckoutSessionId SessionId, CheckoutSessionStatus Status) : IRefusal;

    /// <summary>An earlier step the request needs has not been completed yet.</summary>
    public sealed record CheckoutStepNotCompleted(CheckoutSessionId SessionId, CheckoutStep Step) : IRefusal;

    /// <summary>The requested step lies beyond the step the session has reached.</summary>
    public sealed record CheckoutStepOutOfOrder(CheckoutSessionId SessionId, CheckoutStep Requested, CheckoutStep Current) : IRefusal;

    /// <summary>The order's total is nothing to pay for.</summary>
    public sealed record NothingToPay(CheckoutSessionId SessionId, Money Total) : IRefusal;
}