using System.Collections.Concurrent;
using DcaShop.Product.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Product.Adapter.Outgoing.Pricing;

/// <summary>
/// Stand-in for the Pricing context: answers <see cref="IPricingDataPort"/> from a seeded table. When the
/// Pricing context is ported, this adapter is replaced by one that calls its published Api — the port stays.
/// </summary>
public sealed class InMemoryPricingDataAdapter : IPricingDataPort
{
    private readonly ConcurrentDictionary<ProductId, Money> _prices = new();

    public void Seed(ProductId productId, Money price) => _prices[productId] = price;

    public Task<IReadOnlyDictionary<ProductId, PriceData>> GetPricesAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<ProductId, PriceData>();
        foreach (var id in productIds)
        {
            if (_prices.TryGetValue(id, out var price))
            {
                result[id] = new PriceData(id, price);
            }
        }

        return Task.FromResult<IReadOnlyDictionary<ProductId, PriceData>>(result);
    }
}
