using DcaShop.Checkout.Domain.ReadModel;

namespace DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;

/// <summary><see cref="Session"/> is null when the customer has no active checkout session.</summary>
public sealed record GetActiveCheckoutSessionResult(CheckoutCartSnapshot? Session)
{
    public bool Found => Session is not null;
}