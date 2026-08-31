using DcaShop.Checkout.Application.Shared;
using DcaShop.Checkout.Domain.Model;

namespace DcaShop.Checkout.Adapter.Outgoing.Payment;

/// <summary>
/// Keeps the registered payment providers in memory. The providers themselves come from the container, so
/// adding one is adding an <see cref="IPaymentProvider"/> registration — the checkout needs no change.
/// </summary>
public sealed class InMemoryPaymentProviderRegistry : IPaymentProviderRegistry
{
    private readonly IReadOnlyList<IPaymentProvider> _providers;

    public InMemoryPaymentProviderRegistry(IEnumerable<IPaymentProvider> providers)
    {
        _providers = providers.ToList();
    }

    public async Task<IReadOnlyList<IPaymentProvider>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default)
    {
        var available = new List<IPaymentProvider>(_providers.Count);
        foreach (var provider in _providers)
        {
            if (await provider.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            {
                available.Add(provider);
            }
        }

        return available;
    }

    public Task<IPaymentProvider?> FindAsync(PaymentProviderId providerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_providers.FirstOrDefault(p => p.Id == providerId));
}
