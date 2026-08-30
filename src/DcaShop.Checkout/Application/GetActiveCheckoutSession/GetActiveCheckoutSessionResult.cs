using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.GetActiveCheckoutSession;

public sealed record GetActiveCheckoutSessionResult(CheckoutSessionData? Session)
{
    public bool Found => Session is not null;
}
