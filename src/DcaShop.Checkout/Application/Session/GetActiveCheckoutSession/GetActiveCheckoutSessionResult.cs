using DcaShop.Checkout.Domain.ReadModel;
using DcaShop.Checkout.Application.Shared;

namespace DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;

public sealed record GetActiveCheckoutSessionResult(CheckoutCartSnapshot? Session)
{
    public bool Found => Session is not null;
}
