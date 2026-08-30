using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.GetConfirmedCheckoutSession;

public sealed record GetConfirmedCheckoutSessionResult(CheckoutCartSnapshot? Session)
{
    public bool Found => Session is not null;
}
