namespace DcaShop.Checkout.Application.SubmitPayment;

/// <summary>
/// The provider reference is not part of the input: it is what the payment provider returns when the payment
/// is initiated.
/// </summary>
public sealed record SubmitPaymentCommand(Guid SessionId, string PaymentProviderId);
