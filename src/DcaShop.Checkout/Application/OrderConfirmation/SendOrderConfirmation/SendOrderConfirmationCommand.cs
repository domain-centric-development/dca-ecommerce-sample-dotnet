namespace DcaShop.Checkout.Application.OrderConfirmation.SendOrderConfirmation;

/// <summary>
/// Send the order confirmation of the checkout session <paramref name="SessionId"/>, owned by <paramref name="CustomerId"/>,
/// because the confirmation <paramref name="EventId"/> was delivered — once per event however often it is delivered.
/// </summary>
public sealed record SendOrderConfirmationCommand(Guid EventId, Guid SessionId, string CustomerId);