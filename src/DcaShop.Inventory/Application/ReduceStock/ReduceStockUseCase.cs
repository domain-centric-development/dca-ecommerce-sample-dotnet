using DcaShop.Inventory.Application.Shared;
using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

using Microsoft.Extensions.Logging;

namespace DcaShop.Inventory.Application.ReduceStock;

/// <summary>
/// Reduces the stock of every line of one sale in one transaction. Runs as the reaction to a confirmed checkout, so
/// it reports a missing record or insufficient stock as a failed line instead of throwing — the order stands either
/// way, the discrepancy is an inventory matter, and the remaining lines are still reduced.
/// </summary>
public sealed class ReduceStockUseCase : IReduceStockInputPort
{
    /// <summary>The effect's key in <see cref="IProcessedEvents"/> — stable: a rename would replay old events.</summary>
    private const string Effect = "reduce-stock";

    private readonly IStockLevelRepository _stockLevels;
    private readonly IProcessedEvents _processedEvents;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;
    private readonly ILogger<ReduceStockUseCase> _logger;

    public ReduceStockUseCase(
        IStockLevelRepository stockLevels,
        IProcessedEvents processedEvents,
        IDomainEventPublisher events,
        ITransactionBoundary transactionBoundary,
        ILogger<ReduceStockUseCase> logger)
    {
        _stockLevels = stockLevels;
        _processedEvents = processedEvents;
        _events = events;
        _transactionBoundary = transactionBoundary;
        _logger = logger;
    }

    public Task<ReduceStockResult> ExecuteAsync(ReduceStockCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        return _transactionBoundary.InTransactionAsync(async ct =>
        {
            if (input.EventId is { } eventId && await _processedEvents.ContainsAsync(Effect, eventId, ct).ConfigureAwait(false))
            {
                return ReduceStockResult.Duplicate();
            }

            var outcomes = new List<ReduceStockResult.LineOutcome>();
            foreach (var line in input.Lines)
            {
                outcomes.Add(await ReduceAsync(line, ct).ConfigureAwait(false));
            }

            if (input.EventId is { } processed)
            {
                await _processedEvents.MarkProcessedAsync(Effect, processed, ct).ConfigureAwait(false);
            }

            return new ReduceStockResult(outcomes);
        }, cancellationToken);
    }

    private async Task<ReduceStockResult.LineOutcome> ReduceAsync(ReduceStockCommand.Line line, CancellationToken ct)
    {
        var stockLevel = await _stockLevels.FindByProductIdAsync(new ProductId(line.ProductId), ct).ConfigureAwait(false);
        if (stockLevel is null)
        {
            _logger.LogWarning("Stock level not found for product {ProductId}", line.ProductId);
            return ReduceStockResult.LineOutcome.Failure(line.ProductId, $"Stock level not found for product: {line.ProductId}");
        }

        var previousStock = stockLevel.AvailableQuantity.Value;
        try
        {
            stockLevel.DecreaseStock(line.Quantity);
        }
        catch (InsufficientStockException e)
        {
            // Only the stock rule becomes a failure result. An argument guard from the aggregate would be a
            // malformed call — a defect here, not an answer for the caller — and keeps travelling.
            _logger.LogWarning("Stock too low for product {ProductId}: {Reason}", line.ProductId, e.Message);
            return ReduceStockResult.LineOutcome.Failure(line.ProductId, e.Message);
        }

        await _stockLevels.SaveAsync(stockLevel, ct).ConfigureAwait(false);
        await _events.PublishAndClearEventsAsync(stockLevel, ct).ConfigureAwait(false);

        return ReduceStockResult.LineOutcome.Reduced(line.ProductId, previousStock, stockLevel.AvailableQuantity.Value);
    }
}