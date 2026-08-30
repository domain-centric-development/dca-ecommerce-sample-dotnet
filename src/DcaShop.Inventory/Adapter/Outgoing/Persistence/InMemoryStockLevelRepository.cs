using System.Collections.Concurrent;
using DcaShop.Inventory.Application.Shared;
using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Inventory.Adapter.Outgoing.Persistence;

/// <summary>In-memory stock store with an index by product — demo stand-in for a real database adapter (ADR-001).</summary>
public sealed class InMemoryStockLevelRepository : IStockLevelRepository
{
    private readonly ConcurrentDictionary<StockLevelId, StockLevel> _stockLevels = new();
    private readonly ConcurrentDictionary<ProductId, StockLevelId> _byProduct = new();

    public Task<StockLevel?> FindByIdAsync(StockLevelId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stockLevels.TryGetValue(id, out var stockLevel) ? stockLevel : null);

    public Task<StockLevel?> FindByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        if (_byProduct.TryGetValue(productId, out var id) && _stockLevels.TryGetValue(id, out var stockLevel))
        {
            return Task.FromResult<StockLevel?>(stockLevel);
        }

        return Task.FromResult<StockLevel?>(null);
    }

    public Task<IReadOnlyList<StockLevel>> FindByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        var found = productIds
            .Select(productId => _byProduct.TryGetValue(productId, out var id) && _stockLevels.TryGetValue(id, out var stockLevel) ? stockLevel : null)
            .Where(stockLevel => stockLevel is not null)
            .Select(stockLevel => stockLevel!)
            .ToList();
        return Task.FromResult<IReadOnlyList<StockLevel>>(found);
    }

    public Task<StockLevel> SaveAsync(StockLevel aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        _stockLevels[aggregate.Id] = aggregate;
        _byProduct[aggregate.ProductId] = aggregate.Id;
        return Task.FromResult(aggregate);
    }

    public Task DeleteByIdAsync(StockLevelId id, CancellationToken cancellationToken = default)
    {
        if (_stockLevels.TryRemove(id, out var removed))
        {
            _byProduct.TryRemove(removed.ProductId, out _);
        }

        return Task.CompletedTask;
    }
}
