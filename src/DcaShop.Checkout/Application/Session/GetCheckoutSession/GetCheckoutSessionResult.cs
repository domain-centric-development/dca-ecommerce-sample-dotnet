using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Application.Session.GetCheckoutSession;

/// <summary>
/// <see cref="Session"/> is null when no session with the requested id exists for the customer.
/// <see cref="StepAccess"/> is set only when the query named a step — the domain's decision whether that step may be
/// opened; without a session it sends the customer back to the cart.
/// </summary>
public sealed record GetCheckoutSessionResult(CheckoutCartSnapshot? Session, StepAccess? StepAccess = null)
{
    public bool Found => Session is not null;
}