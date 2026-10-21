namespace DcaShop.Checkout.Application.CheckoutCompletion.ConfirmCheckout;

/// <summary>Places the order the review showed, as its <paramref name="Fingerprint"/> names it.</summary>
public sealed record ConfirmCheckoutCommand(Guid SessionId, string CustomerId, string Fingerprint);