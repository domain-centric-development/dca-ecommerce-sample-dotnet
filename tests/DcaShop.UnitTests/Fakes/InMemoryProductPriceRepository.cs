using System.Collections.Concurrent;

using DcaShop.Pricing.Application.Shared;
using DcaShop.Pricing.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Pricing.Adapter.Outgoing.Persistence;

/// <summary>
/// In-memory price store with an index by product — the unit tests' stand-in for the database adapter, storing and
/// handing out copies as a database does.
/// </summary>
public sealed class InMemoryProductPriceRepository : IProductPriceRepository
{
    private readonly ConcurrentDictionary<PriceId, ProductPrice> _prices = new();
    private readonly ConcurrentDictionary<ProductId, PriceId> _byProduct = new();

    public Task<ProductPrice?> FindByIdAsync(PriceId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_prices.TryGetValue(id, out var price) ? Copy(price) : null);

    public Task<ProductPrice?> FindByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        if (_byProduct.TryGetValue(productId, out var priceId) && _prices.TryGetValue(priceId, out var price))
        {
            return Task.FromResult<ProductPrice?>(Copy(price));
        }

        return Task.FromResult<ProductPrice?>(null);
    }

    public Task<IReadOnlyList<ProductPrice>> FindByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        var found = productIds
            .Select(id => _byProduct.TryGetValue(id, out var priceId) && _prices.TryGetValue(priceId, out var price) ? price : null)
            .Where(price => price is not null)
            .Select(price => Copy(price!))
            .ToList();
        return Task.FromResult<IReadOnlyList<ProductPrice>>(found);
    }

    public Task<ProductPrice> SaveAsync(ProductPrice aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        _prices[aggregate.Id] = Copy(aggregate);
        _byProduct[aggregate.ProductId] = aggregate.Id;
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(PriceId id, CancellationToken cancellationToken = default)
    {
        if (_prices.TryRemove(id, out var removed))
        {
            _byProduct.TryRemove(removed.ProductId, out _);
        }

        return Task.CompletedTask;
    }

    private static ProductPrice Copy(ProductPrice price) =>
        ProductPrice.Reconstitute(price.Id, price.ProductId, price.CurrentPrice, price.EffectiveFrom);
}