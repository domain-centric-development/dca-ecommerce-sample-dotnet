using DcaShop.Checkout.Domain.Model;
using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;

/// <summary>
/// <see cref="Session"/> is null when the customer has no active checkout session. <see cref="StepAccess"/> is set
/// only when the query named a step — the domain's decision whether that step may be opened.
/// </summary>
public sealed record GetActiveCheckoutSessionResult(CheckoutCartSnapshot? Session, StepAccess? StepAccess)
{
    public bool Found => Session is not null;
}
