using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitDelivery;

/// <summary>What the caller needs for its next step: the session and where it stands. The page itself comes from a session query.</summary>
public sealed record SubmitDeliveryResult(Guid SessionId, CheckoutStep CurrentStep, CheckoutSessionStatus Status)
{
    public static SubmitDeliveryResult From(CheckoutSession session) => new(session.Id.Value, session.CurrentStep, session.Status);
}
