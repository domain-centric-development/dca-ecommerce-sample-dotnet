using DcaShop.Inventory.Application.GetStockForProducts;
using DcaShop.Inventory.Application.ReduceStock;
using DcaShop.Inventory.Application.SetStockLevel;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Inventory.Api;

/// <summary>Open Host Service of Inventory: how much of a product is on hand, for other bounded contexts.</summary>
[OpenHostService("Inventory", Description = "Provides stock information for other bounded contexts")]
public sealed class InventoryService
{
    private readonly IGetStockForProductsInputPort _getStock;
    private readonly ISetStockLevelInputPort _setStockLevel;
    private readonly IReduceStockInputPort _reduceStock;

    public InventoryService(
        IGetStockForProductsInputPort getStock,
        ISetStockLevelInputPort setStockLevel,
        IReduceStockInputPort reduceStock)
    {
        _getStock = getStock;
        _setStockLevel = setStockLevel;
        _reduceStock = reduceStock;
    }

    public sealed record StockInfo(ProductId ProductId, int AvailableStock, bool IsAvailable);

    public async Task<IReadOnlyDictionary<ProductId, StockInfo>> GetStockAsync(IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<ProductId, StockInfo>();
        }

        var result = await _getStock.ExecuteAsync(new GetStockForProductsQuery(productIds), cancellationToken).ConfigureAwait(false);
        return result.Stocks.ToDictionary(
            entry => entry.Key,
            entry => new StockInfo(entry.Value.ProductId, entry.Value.AvailableStock, entry.Value.IsAvailable));
    }

    public async Task<StockInfo?> GetStockAsync(ProductId productId, CancellationToken cancellationToken = default)
    {
        var stocks = await GetStockAsync(new[] { productId }, cancellationToken).ConfigureAwait(false);
        return stocks.TryGetValue(productId, out var stock) ? stock : null;
    }

    public async Task<bool> HasStockAsync(ProductId productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return true;
        }

        var stock = await GetStockAsync(productId, cancellationToken).ConfigureAwait(false);
        return stock is not null && stock.AvailableStock >= quantity;
    }

    public Task SetStockLevelAsync(ProductId productId, int quantity, CancellationToken cancellationToken = default) =>
        _setStockLevel.ExecuteAsync(new SetStockLevelCommand(productId.Value, quantity), cancellationToken);

    public Task<ReduceStockResult> ReduceStockAsync(ProductId productId, int quantity, CancellationToken cancellationToken = default) =>
        _reduceStock.ExecuteAsync(new ReduceStockCommand(productId.Value, quantity), cancellationToken);
}
