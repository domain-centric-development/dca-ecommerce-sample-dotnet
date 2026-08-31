using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Checkout.Adapter.Outgoing.Payment;

/// <summary>
/// Stands in for a payment service provider: every payment is approved and tracked under a made-up
/// <c>mock-{guid}</c> reference. Enough to drive the checkout end to end without a real gateway.
/// </summary>
public sealed class MockPaymentProvider : IPaymentProvider
{
    public static readonly PaymentProviderId ProviderId = PaymentProviderId.Of("mock");

    private const string ReferencePrefix = "mock-";

    private bool _available = true;

    public PaymentProviderId Id => ProviderId;

    public string DisplayName => "Mock Payment (Test)";

    public Task<IPaymentProvider.PaymentResult> InitiatePaymentAsync(CheckoutSessionId sessionId, Money amount, CancellationToken cancellationToken = default) =>
        Task.FromResult(_available
            ? IPaymentProvider.PaymentResult.Succeeded(ReferencePrefix + Guid.NewGuid())
            : Unavailable());

    public Task<IPaymentProvider.PaymentResult> ConfirmPaymentAsync(string providerReference, CancellationToken cancellationToken = default) =>
        Task.FromResult(Settle(providerReference));

    public Task<IPaymentProvider.PaymentResult> CancelPaymentAsync(string providerReference, CancellationToken cancellationToken = default) =>
        Task.FromResult(Settle(providerReference));

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult(_available);

    /// <summary>Lets a test drive the checkout against a provider that is down.</summary>
    public void SetAvailable(bool available) => _available = available;

    private IPaymentProvider.PaymentResult Settle(string providerReference)
    {
        if (!_available)
        {
            return Unavailable();
        }

        return providerReference.StartsWith(ReferencePrefix, StringComparison.Ordinal)
            ? IPaymentProvider.PaymentResult.Succeeded(providerReference)
            : IPaymentProvider.PaymentResult.Failed($"Invalid mock payment reference: {providerReference}");
    }

    private static IPaymentProvider.PaymentResult Unavailable() =>
        IPaymentProvider.PaymentResult.Failed("Mock payment provider is currently unavailable");
}
