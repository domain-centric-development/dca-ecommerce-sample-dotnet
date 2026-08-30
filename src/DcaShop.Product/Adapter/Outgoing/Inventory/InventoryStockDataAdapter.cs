using DcaShop.Inventory.Api;
using DcaShop.Product.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;

namespace DcaShop.Product.Adapter.Outgoing.Inventory;

/// <summary>
/// Anti-corruption layer to the Inventory context: calls its published Api and translates <c>StockInfo</c> into the
/// catalog's own <see cref="StockData"/>. A product without a stock record is simply absent from the answer.
/// </summary>
public sealed class InventoryStockDataAdapter : IProductStockDataPort
{
    private readonly InventoryService _inventory;

    public InventoryStockDataAdapter(InventoryService inventory)
    {
        _inventory = inventory;
    }

    public async Task<IReadOnlyDictionary<ProductId, StockData>> GetStockDataAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        var stocks = await _inventory.GetStockAsync(productIds, cancellationToken).ConfigureAwait(false);
        return stocks.ToDictionary(entry => entry.Key, entry => new StockData(entry.Value.ProductId, entry.Value.AvailableStock, entry.Value.IsAvailable));
    }
}
