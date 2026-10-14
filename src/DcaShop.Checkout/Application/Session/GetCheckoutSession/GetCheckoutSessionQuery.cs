using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.Session.GetCheckoutSession;

/// <summary>With a <see cref="RequestedStep"/> the result also says whether that step may be opened.</summary>
public sealed record GetCheckoutSessionQuery(Guid SessionId, string CustomerId, CheckoutStep? RequestedStep = null)
{
    /// <summary>The session, and the domain's decision whether <paramref name="step"/> may be opened in it.</summary>
    public static GetCheckoutSessionQuery ForStep(Guid sessionId, string customerId, CheckoutStep step) =>
        new(sessionId, customerId, step);
}