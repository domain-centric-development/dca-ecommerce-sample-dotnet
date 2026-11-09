using System.Collections.Concurrent;

using DcaShop.Inventory.Application.Shared;
using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Inventory.Adapter.Outgoing.Persistence;

/// <summary>
/// In-memory stock store with an index by product — the unit tests' stand-in for the database adapter, storing and
/// handing out copies as a database does.
/// </summary>
public sealed class InMemoryStockLevelRepository : IStockLevelRepository
{
    private readonly ConcurrentDictionary<StockLevelId, StockLevel> _stockLevels = new();
    private readonly ConcurrentDictionary<ProductId, StockLevelId> _byProduct = new();

    public Task<StockLevel?> FindByIdAsync(StockLevelId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_stockLevels.TryGetValue(id, out var stockLevel) ? Copy(stockLevel) : null);

    public Task<StockLevel?> FindByProductIdAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        if (_byProduct.TryGetValue(productId, out var id) && _stockLevels.TryGetValue(id, out var stockLevel))
        {
            return Task.FromResult<StockLevel?>(Copy(stockLevel));
        }

        return Task.FromResult<StockLevel?>(null);
    }

    public Task<IReadOnlyList<StockLevel>> FindByProductIdsAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        var found = productIds
            .Select(productId => _byProduct.TryGetValue(productId, out var id) && _stockLevels.TryGetValue(id, out var stockLevel) ? stockLevel : null)
            .Where(stockLevel => stockLevel is not null)
            .Select(stockLevel => Copy(stockLevel!))
            .ToList();
        return Task.FromResult<IReadOnlyList<StockLevel>>(found);
    }

    public Task<IReadOnlyList<StockLevel>> FindAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StockLevel>>(_stockLevels.Values.Select(Copy).ToList());

    public Task<StockLevel> SaveAsync(StockLevel aggregate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        _stockLevels[aggregate.Id] = Copy(aggregate);
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

    private static StockLevel Copy(StockLevel stockLevel) => StockLevel.Reconstitute(
        stockLevel.Id, stockLevel.ProductId, stockLevel.AvailableQuantity.Value, stockLevel.ReservedQuantity.Value);
}