using System.Collections.Concurrent;
using DcaShop.Product.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Product.Adapter.Outgoing.Inventory;

/// <summary>
/// Stand-in for the Inventory context: answers <see cref="IProductStockDataPort"/> from a seeded table. When the
/// Inventory context is ported, this adapter is replaced by one that calls its published Api — the port stays.
/// </summary>
public sealed class InMemoryStockDataAdapter : IProductStockDataPort
{
    private readonly ConcurrentDictionary<ProductId, int> _stock = new();

    public void Seed(ProductId productId, int availableStock) => _stock[productId] = availableStock;

    public Task<IReadOnlyDictionary<ProductId, StockData>> GetStockDataAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<ProductId, StockData>();
        foreach (var id in productIds)
        {
            if (_stock.TryGetValue(id, out var stock))
            {
                result[id] = new StockData(id, stock, stock > 0);
            }
        }

        return Task.FromResult<IReadOnlyDictionary<ProductId, StockData>>(result);
    }
}
