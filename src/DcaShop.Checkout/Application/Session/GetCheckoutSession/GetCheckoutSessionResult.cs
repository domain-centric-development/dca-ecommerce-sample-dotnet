using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.Session.GetCheckoutSession;

/// <summary><see cref="Session"/> is null when no session with the requested id exists.</summary>
public sealed record GetCheckoutSessionResult(CheckoutCartSnapshot? Session)
{
    public bool Found => Session is not null;
}
