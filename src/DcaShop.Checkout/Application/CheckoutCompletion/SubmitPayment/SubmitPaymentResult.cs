using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitPayment;

/// <summary>
/// What the caller needs for its next step: the session and where it stands, and whether the payment the session held
/// already covered it, so nothing new was requested. The page itself comes from a session query.
/// </summary>
public sealed record SubmitPaymentResult(Guid SessionId, CheckoutStep CurrentStep, CheckoutSessionStatus Status, bool PaymentAlreadyAuthorized = false)
{
    public static SubmitPaymentResult From(CheckoutSession session) => new(session.Id.Value, session.CurrentStep, session.Status);
}