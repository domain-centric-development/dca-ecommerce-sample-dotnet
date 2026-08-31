using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Checkout.Application.Shared;

/// <summary>
/// Output port for payment processing. One implementation per payment system (Stripe, PayPal, bank transfer …);
/// the checkout works against this contract and knows none of them.
/// </summary>
public interface IPaymentProvider : IOutputPort
{
    /// <summary>The unique identifier of this provider, as the shopper selects it.</summary>
    PaymentProviderId Id { get; }

    /// <summary>A human-readable name for this provider.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Creates a payment intent or its equivalent with the provider. The result carries the provider-specific
    /// reference the payment is tracked and completed by.
    /// </summary>
    Task<IPaymentProvider.PaymentResult> InitiatePaymentAsync(CheckoutSessionId sessionId, Money amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a payment that was initiated earlier. Providers that complete the payment on initiation may
    /// answer without doing anything.
    /// </summary>
    Task<IPaymentProvider.PaymentResult> ConfirmPaymentAsync(string providerReference, CancellationToken cancellationToken = default);

    /// <summary>Cancels a payment that was initiated earlier, releasing any funds the provider holds.</summary>
    Task<IPaymentProvider.PaymentResult> CancelPaymentAsync(string providerReference, CancellationToken cancellationToken = default);

    /// <summary>Whether the provider can process payments right now.</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>The outcome of a payment operation. Nested in the port because it is part of its contract.</summary>
    public sealed record PaymentResult(bool Success, string? ProviderReference, string? ErrorMessage)
    {
        public static PaymentResult Succeeded(string providerReference) => new(true, providerReference, null);

        public static PaymentResult Failed(string errorMessage) => new(false, null, errorMessage);
    }
}
