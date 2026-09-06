using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;

/// <summary>What the caller needs for its next step: the session and where it stands. The page itself comes from a session query.</summary>
public sealed record ConfirmCheckoutResult(Guid SessionId, CheckoutStep CurrentStep, CheckoutSessionStatus Status)
{
    public static ConfirmCheckoutResult From(CheckoutSession session) => new(session.Id.Value, session.CurrentStep, session.Status);
}
