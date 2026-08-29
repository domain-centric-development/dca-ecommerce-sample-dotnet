using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Adapter.Outgoing.Payment;

/// <summary>Stands in for the payment service provider: a fixed list of providers, no real gateway call.</summary>
public sealed class InMemoryPaymentProviderRegistry : IPaymentProviderRegistry
{
    private static readonly IReadOnlyList<PaymentProviderInfo> Providers = new[]
    {
        new PaymentProviderInfo(PaymentProviderId.Of("invoice"), "Invoice", "Pay within 14 days after delivery"),
        new PaymentProviderInfo(PaymentProviderId.Of("paypal"), "PayPal", "Redirect to PayPal (mock)"),
        new PaymentProviderInfo(PaymentProviderId.Of("stripe"), "Credit Card", "Card payment via Stripe (mock)"),
    };

    public Task<IReadOnlyList<PaymentProviderInfo>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Providers);

    public Task<PaymentProviderInfo?> FindAsync(PaymentProviderId providerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Providers.FirstOrDefault(p => p.Id == providerId));
}
