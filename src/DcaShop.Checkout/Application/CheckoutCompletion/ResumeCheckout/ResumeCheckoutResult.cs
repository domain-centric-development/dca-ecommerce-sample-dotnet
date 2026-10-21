using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.CheckoutCompletion.ResumeCheckout;

/// <summary>
/// The resumed session and where it stands — <see cref="SessionId"/> is null when the customer's cart has no open
/// checkout. The page itself comes from a session query.
/// </summary>
public sealed record ResumeCheckoutResult(Guid? SessionId, CheckoutStep? CurrentStep)
{
    public bool Resumed => SessionId is not null;

    public static ResumeCheckoutResult NothingToResume() => new(null, null);

    public static ResumeCheckoutResult From(CheckoutSession session) => new(session.Id.Value, session.CurrentStep);
}