using DcaShop.Inventory.Application.GetStockForProducts;
using DcaShop.Inventory.Application.ProvisionInitialStock;
using DcaShop.Inventory.Application.ReduceStock;
using DcaShop.Inventory.Application.ReleaseStock;
using DcaShop.Inventory.Application.ReserveStock;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Ddd.Strategic.Relationships;

namespace DcaShop.Inventory.Api;

/// <summary>Open Host Service of Inventory: how much of a product is on hand, for other bounded contexts.</summary>
[OpenHostService("Inventory", Description = "Provides stock information for other bounded contexts")]
public sealed class InventoryService
{
    private readonly IGetStockForProductsInputPort _getStock;
    private readonly IProvisionInitialStockInputPort _provisionInitialStock;
    private readonly IReduceStockInputPort _reduceStock;
    private readonly IReserveStockInputPort _reserveStock;
    private readonly IReleaseStockInputPort _releaseStock;

    public InventoryService(
        IGetStockForProductsInputPort getStock,
        IProvisionInitialStockInputPort provisionInitialStock,
        IReduceStockInputPort reduceStock,
        IReserveStockInputPort reserveStock,
        IReleaseStockInputPort releaseStock)
    {
        _getStock = getStock;
        _provisionInitialStock = provisionInitialStock;
        _reduceStock = reduceStock;
        _reserveStock = reserveStock;
        _releaseStock = releaseStock;
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

    public Task ProvisionInitialStockAsync(ProductId productId, int quantity, CancellationToken cancellationToken = default) =>
        _provisionInitialStock.ExecuteAsync(new ProvisionInitialStockCommand(productId.Value, quantity), cancellationToken);

    public Task<ReduceStockResult> ReduceStockAsync(ProductId productId, int quantity, CancellationToken cancellationToken = default) =>
        _reduceStock.ExecuteAsync(new ReduceStockCommand([new ReduceStockCommand.Line(productId.Value, quantity)]), cancellationToken);

    /// <summary>How much of a product an order needs.</summary>
    public sealed record StockLine(ProductId ProductId, int Quantity);

    /// <summary>A product the unreserved stock does not cover, and how much of it is left to promise.</summary>
    public sealed record StockShortage(ProductId ProductId, int Unreserved);

    /// <summary>
    /// Reserves the stock of an order about to be placed, every line or none; the answer names the products the
    /// unreserved stock does not cover, empty when reserved. The order's later stock reduction consumes the reservation.
    /// </summary>
    public async Task<IReadOnlyList<StockShortage>> ReserveStockAsync(IReadOnlyCollection<StockLine> lines, CancellationToken cancellationToken = default)
    {
        var reservation = await _reserveStock.ExecuteAsync(
            new ReserveStockCommand(lines.Select(line => new ReserveStockCommand.Line(line.ProductId.Value, line.Quantity)).ToList()),
            cancellationToken).ConfigureAwait(false);
        return reservation.Shortages.Select(shortage => new StockShortage(new ProductId(shortage.ProductId), shortage.Unreserved)).ToList();
    }

    /// <summary>Gives back what <see cref="ReserveStockAsync"/> reserved for an order that was then not placed.</summary>
    public Task ReleaseStockAsync(IReadOnlyCollection<StockLine> lines, CancellationToken cancellationToken = default) =>
        _releaseStock.ExecuteAsync(
            new ReleaseStockCommand(lines.Select(line => new ReleaseStockCommand.Line(line.ProductId.Value, line.Quantity)).ToList()),
            cancellationToken);
}