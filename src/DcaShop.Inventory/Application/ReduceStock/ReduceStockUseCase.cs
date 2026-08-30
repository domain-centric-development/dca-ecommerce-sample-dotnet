using DcaShop.Inventory.Application.Shared;
using DcaShop.SharedKernel.Domain.Model;
using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;
using Microsoft.Extensions.Logging;

namespace DcaShop.Inventory.Application.ReduceStock;

/// <summary>
/// Reduces the stock of one product. Runs as the reaction to a confirmed checkout, so it reports a missing record
/// or insufficient stock as a failed result instead of throwing — the order stands either way, the discrepancy is
/// an inventory matter.
/// </summary>
public sealed class ReduceStockUseCase : IReduceStockInputPort
{
    private readonly IStockLevelRepository _stockLevels;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;
    private readonly ILogger<ReduceStockUseCase> _logger;

    public ReduceStockUseCase(
        IStockLevelRepository stockLevels,
        IDomainEventPublisher events,
        ITransactionBoundary transactionBoundary,
        ILogger<ReduceStockUseCase> logger)
    {
        _stockLevels = stockLevels;
        _events = events;
        _transactionBoundary = transactionBoundary;
        _logger = logger;
    }

    public Task<ReduceStockResult> ExecuteAsync(ReduceStockCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var productId = new ProductId(input.ProductId);

        return _transactionBoundary.InTransactionAsync(async ct =>
        {
            var stockLevel = await _stockLevels.FindByProductIdAsync(productId, ct).ConfigureAwait(false);
            if (stockLevel is null)
            {
                _logger.LogWarning("Stock level not found for product {ProductId}", input.ProductId);
                return ReduceStockResult.Failure(input.ProductId, $"Stock level not found for product: {input.ProductId}");
            }

            var previousStock = stockLevel.AvailableQuantity.Value;
            try
            {
                stockLevel.DecreaseStock(input.Quantity);
            }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException)
            {
                _logger.LogWarning(e, "Cannot reduce stock for product {ProductId} by {Quantity}", input.ProductId, input.Quantity);
                return ReduceStockResult.Failure(input.ProductId, e.Message);
            }

            await _stockLevels.SaveAsync(stockLevel, ct).ConfigureAwait(false);
            await _events.PublishAndClearEventsAsync(stockLevel, ct).ConfigureAwait(false);

            return ReduceStockResult.Reduced(input.ProductId, previousStock, stockLevel.AvailableQuantity.Value);
        }, cancellationToken);
    }
}
