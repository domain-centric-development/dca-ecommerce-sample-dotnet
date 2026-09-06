using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.Session.StartCheckout;

/// <summary>What the caller needs for its next step: the session and where it stands. The page itself comes from a session query.</summary>
public sealed record StartCheckoutResult(Guid SessionId, CheckoutStep CurrentStep, CheckoutSessionStatus Status)
{
    public static StartCheckoutResult From(CheckoutSession session) => new(session.Id.Value, session.CurrentStep, session.Status);
}
