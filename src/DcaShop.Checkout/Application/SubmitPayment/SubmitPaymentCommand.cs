namespace DcaShop.Checkout.Application.SubmitPayment;

public sealed record SubmitPaymentCommand(Guid SessionId, string PaymentProviderId, string? ProviderReference = null);
