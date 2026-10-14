using DcaShop.Inventory.Application.Shared;
using DcaShop.Inventory.Domain.Model;
using DcaShop.SharedKernel.Domain.Model;

using DomainCentric.BuildingBlocks.Application.Transactions;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DcaShop.Inventory.Application.ProvisionInitialStock;

/// <summary>Creates the stock record on first use and corrects it afterwards. Whole use case is local: one short transaction.</summary>
public sealed class ProvisionInitialStockUseCase : IProvisionInitialStockInputPort
{
    /// <summary>The effect's key in <see cref="IProcessedEvents"/> — stable: a rename would replay old events.</summary>
    private const string Effect = "set-stock-level";

    private readonly IStockLevelRepository _stockLevels;
    private readonly IProcessedEvents _processedEvents;
    private readonly IDomainEventPublisher _events;
    private readonly ITransactionBoundary _transactionBoundary;

    public ProvisionInitialStockUseCase(IStockLevelRepository stockLevels, IProcessedEvents processedEvents, IDomainEventPublisher events, ITransactionBoundary transactionBoundary)
    {
        _stockLevels = stockLevels;
        _processedEvents = processedEvents;
        _events = events;
        _transactionBoundary = transactionBoundary;
    }

    public Task<ProvisionInitialStockResult> ExecuteAsync(ProvisionInitialStockCommand input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var productId = new ProductId(input.ProductId);

        return _transactionBoundary.InTransactionAsync(async ct =>
        {
            var existing = await _stockLevels.FindByProductIdAsync(productId, ct).ConfigureAwait(false);
            if (input.EventId is { } eventId && await _processedEvents.ContainsAsync(Effect, eventId, ct).ConfigureAwait(false))
            {
                return existing is null
                    ? new ProvisionInitialStockResult(Guid.Empty, input.ProductId, 0, 0, Created: false, AlreadyApplied: true)
                    : ResultOf(existing, created: false, alreadyApplied: true);
            }

            StockLevel stockLevel;
            bool created;
            if (existing is not null)
            {
                stockLevel = existing;
                stockLevel.AdjustStockTo(input.Quantity);
                created = false;
            }
            else
            {
                stockLevel = StockLevel.Create(productId, input.Quantity);
                created = true;
            }

            await _stockLevels.SaveAsync(stockLevel, ct).ConfigureAwait(false);
            await _events.PublishAndClearEventsAsync(stockLevel, ct).ConfigureAwait(false);
            if (input.EventId is { } processed)
            {
                await _processedEvents.MarkProcessedAsync(Effect, processed, ct).ConfigureAwait(false);
            }

            return ResultOf(stockLevel, created, alreadyApplied: false);
        }, cancellationToken);
    }

    private static ProvisionInitialStockResult ResultOf(StockLevel stockLevel, bool created, bool alreadyApplied) =>
        new(
            stockLevel.Id.Value,
            stockLevel.ProductId.Value,
            stockLevel.AvailableQuantity.Value,
            stockLevel.ReservedQuantity.Value,
            created,
            alreadyApplied);
}