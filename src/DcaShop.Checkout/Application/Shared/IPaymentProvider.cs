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

    /// <summary>How a payment operation ended: done, declined by the provider, or no usable answer from it.</summary>
    public enum PaymentOutcome
    {
        Succeeded,
        Refused,
        Unavailable,
    }

    /// <summary>
    /// The outcome of a payment operation. Nested in the port because it is part of its contract. Only the three
    /// factories create one, so a success always carries a reference and a failure always carries a reason.
    /// </summary>
    public sealed record PaymentResult
    {
        private PaymentResult(PaymentOutcome outcome, string? providerReference, string? errorMessage)
        {
            Outcome = outcome;
            ProviderReference = providerReference;
            ErrorMessage = errorMessage;
        }

        public PaymentOutcome Outcome { get; }

        public string? ProviderReference { get; }

        public string? ErrorMessage { get; }

        public bool Success => Outcome == PaymentOutcome.Succeeded;

        public static PaymentResult Succeeded(string providerReference) => new(PaymentOutcome.Succeeded, providerReference, null);

        /// <summary>
        /// The provider declined the operation, or does not offer it at all; asking again with the same means will
        /// not change that.
        /// </summary>
        public static PaymentResult Refused(string reason) => new(PaymentOutcome.Refused, null, reason);

        /// <summary>The provider could not be reached, did not answer in time or answered outside its contract.</summary>
        public static PaymentResult Unavailable(string reason) => new(PaymentOutcome.Unavailable, null, reason);
    }
}