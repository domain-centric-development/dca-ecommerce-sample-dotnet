using DcaShop.Inventory.Application.ReduceStock;
using DcaShop.Inventory.Application.Shared;
using DcaShop.Inventory.Events;
using DcaShop.SharedKernel.Infrastructure.Events;

using Microsoft.Extensions.Logging;

namespace DcaShop.Inventory.Adapter.Incoming.Event;

/// <summary>
/// Reduces stock for every line of a confirmed order when an integration event implementing
/// <see cref="IStockReductionTrigger"/> arrives. A line that cannot be reduced is logged and does not stop the
/// remaining lines — the reduction is a reaction, not part of the checkout's transaction. Idempotent: an event id
/// already handled is skipped (<see cref="IProcessedEvents"/>), so a redelivered order never reduces stock twice.
/// </summary>
public sealed class StockReductionEventConsumer : EventListener<IStockReductionTrigger>
{
    private readonly IReduceStockInputPort _reduceStock;
    private readonly IProcessedEvents _processedEvents;
    private readonly ILogger<StockReductionEventConsumer> _logger;

    public StockReductionEventConsumer(IReduceStockInputPort reduceStock, IProcessedEvents processedEvents, ILogger<StockReductionEventConsumer> logger)
    {
        _reduceStock = reduceStock;
        _processedEvents = processedEvents;
        _logger = logger;
    }

    protected override async Task OnAsync(IStockReductionTrigger @event, CancellationToken cancellationToken)
    {
        if (await _processedEvents.ContainsAsync(@event.EventId, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("Skipping event {EventId}: already handled", @event.EventId);
            return;
        }

        _logger.LogInformation("Reducing stock for {LineItemCount} line items after checkout confirmation", @event.OrderLineItems.Count);
        foreach (var item in @event.OrderLineItems)
        {
            var result = await _reduceStock.ExecuteAsync(new ReduceStockCommand(item.ProductId.Value, item.Quantity), cancellationToken).ConfigureAwait(false);
            if (!result.Success)
            {
                _logger.LogWarning("Stock reduction for product {ProductId} failed: {Error}", item.ProductId, result.ErrorMessage);
            }
        }
        await _processedEvents.MarkProcessedAsync(@event.EventId, cancellationToken).ConfigureAwait(false);
    }
}