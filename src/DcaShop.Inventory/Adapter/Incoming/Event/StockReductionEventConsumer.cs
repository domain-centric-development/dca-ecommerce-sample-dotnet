using DcaShop.Inventory.Application.ReduceStock;
using DcaShop.Inventory.Events;
using DcaShop.SharedKernel.Infrastructure.Events;
using Microsoft.Extensions.Logging;

namespace DcaShop.Inventory.Adapter.Incoming.Event;

/// <summary>
/// Reduces stock for every line of a confirmed order when an integration event implementing
/// <see cref="IStockReductionTrigger"/> arrives. A line that cannot be reduced is logged and does not stop the
/// remaining lines — the reduction is a reaction, not part of the checkout's transaction.
/// </summary>
public sealed class StockReductionEventConsumer : EventListener<IStockReductionTrigger>
{
    private readonly IReduceStockInputPort _reduceStock;
    private readonly ILogger<StockReductionEventConsumer> _logger;

    public StockReductionEventConsumer(IReduceStockInputPort reduceStock, ILogger<StockReductionEventConsumer> logger)
    {
        _reduceStock = reduceStock;
        _logger = logger;
    }

    protected override async Task OnAsync(IStockReductionTrigger @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reducing stock for {LineItemCount} line items after checkout confirmation", @event.OrderLineItems.Count);
        foreach (var item in @event.OrderLineItems)
        {
            var result = await _reduceStock.ExecuteAsync(new ReduceStockCommand(item.ProductId.Value, item.Quantity), cancellationToken).ConfigureAwait(false);
            if (!result.Success)
            {
                _logger.LogWarning("Stock reduction for product {ProductId} failed: {Error}", item.ProductId, result.ErrorMessage);
            }
        }
    }
}
