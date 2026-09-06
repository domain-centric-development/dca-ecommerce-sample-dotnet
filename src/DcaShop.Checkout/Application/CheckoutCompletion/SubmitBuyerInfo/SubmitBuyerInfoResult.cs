using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.CheckoutCompletion.SubmitBuyerInfo;

/// <summary>What the caller needs for its next step: the session and where it stands. The page itself comes from a session query.</summary>
public sealed record SubmitBuyerInfoResult(Guid SessionId, CheckoutStep CurrentStep, CheckoutSessionStatus Status)
{
    public static SubmitBuyerInfoResult From(CheckoutSession session) => new(session.Id.Value, session.CurrentStep, session.Status);
}
