namespace DcaShop.Checkout.Application.OrderConfirmation.SendOrderConfirmation;

/// <summary>The order whose confirmation was sent, and whether it had been sent before this delivery.</summary>
public sealed record SendOrderConfirmationResult(Guid SessionId, bool AlreadySent);