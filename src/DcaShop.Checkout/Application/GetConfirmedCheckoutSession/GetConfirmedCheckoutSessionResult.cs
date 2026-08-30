using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.GetConfirmedCheckoutSession;

public sealed record GetConfirmedCheckoutSessionResult(CheckoutSessionData? Session)
{
    public bool Found => Session is not null;
}
