using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Application.Session.GetActiveCheckoutSession;

/// <summary>With a <see cref="RequestedStep"/> the result also says whether that step may be opened.</summary>
public sealed record GetActiveCheckoutSessionQuery(string CustomerId, CheckoutStep? RequestedStep = null);
