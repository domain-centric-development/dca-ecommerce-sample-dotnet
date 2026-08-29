using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.GetCheckoutSession;

/// <summary><see cref="Session"/> is null when no session with the requested id exists.</summary>
public sealed record GetCheckoutSessionResult(CheckoutSessionData? Session)
{
    public bool Found => Session is not null;
}
